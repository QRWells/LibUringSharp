using System.Numerics;
using System.Runtime.InteropServices;
using LibUringSharp.Completion;
using LibUringSharp.Enums;
using LibUringSharp.Exceptions;
using LibUringSharp.Submission;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp;

public sealed partial class Ring : IDisposable
{
    private readonly Queue<Action<Submission.Submission>> _pendingSubmissions = new();

    /// <summary>
    ///     Constructs a new <see cref="Ring" /> with the given number of entries and flags
    /// </summary>
    /// <param name="entries">Entries in the ring, will be rounded up to the nearest power of 2</param>
    /// <param name="flags">Flags to pass to io_uring_setup</param>
    /// <exception cref="PlatformNotSupportedException">Thrown if the current OS is not Linux</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="entries" /> is 0</exception>
    /// <exception cref="RingInitFailedException">Thrown if the ring failed to initialize</exception>
    public Ring(uint entries, RingSetup flags = RingSetup.None)
    {
        // Check if we're on Linux
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            throw new PlatformNotSupportedException("io_uring is only supported on Linux");

        if (entries == 0)
            throw new ArgumentException("entries must be greater than 0", nameof(entries));

        entries = BitOperations.RoundUpToPowerOf2(entries);

        io_uring_params p = default;
        p.flags = (uint)flags;

        _ringFd = io_uring_setup(entries, ref p);

        if (_ringFd.IsInvalid)
            throw new RingInitFailedException("io_uring_setup failed");

        var (sqSize, cqSize) = ComputeRingSize(in p);
        try
        {
            _submissionQueue = MapSubmissionQueue(in p, sqSize, out _sqMMapHandle, out _sqeMMapHandle);
            _completionQueue = MapCompletionQueue(in p, cqSize, out _cqMMapHandle);
        }
        catch (Exception)
        {
            Dispose();
            throw new RingInitFailedException("Failed to map queues");
        }

        _flags = (RingSetup)p.flags;
        _enterRingFd = _ringFd;
        _features = (RingFeature)p.features;
    }

    public bool IsKernelIoPolling => _flags.HasFlag(RingSetup.KernelIoPolling);
    public bool IsKernelSubmissionQueuePolling => _flags.HasFlag(RingSetup.KernelSubmissionQueuePolling);

    internal bool IsInterruptRegistered => _intFlags.HasFlag(RingInterrupt.RegRing);

    public void Dispose()
    {
        _sqMMapHandle.Dispose();
        _sqeMMapHandle.Dispose();
        _cqMMapHandle.Dispose();
        if (_intFlags.HasFlag(RingInterrupt.RegRing))
            UnregisterRingFd();
        _ringFd.Dispose();
        _enterRingFd.Dispose();
        foreach (var i in _bufferGroups.Keys)
            _bufferGroups[i].Release();
    }

    /// <summary>
    ///     Map the submission queue and submission queue entries
    /// </summary>
    /// <param name="p">parameters of the <see cref="Ring" /></param>
    /// <param name="ringSize">size of the submission queue ring</param>
    /// <param name="sqHandle">out <see cref="MMapHandle" /> of the submission queue</param>
    /// <param name="sqeHandle">out <see cref="MMapHandle" /> of the submission queue entries</param>
    /// <returns></returns>
    /// <exception cref="MapQueueFailedException">Throw if <see cref="Linux.LibC.MemMap" /> fails.</exception>
    private SubmissionQueue MapSubmissionQueue(in io_uring_params p, uint ringSize, out MMapHandle sqHandle,
        out MMapHandle sqeHandle)
    {
        sqHandle = MemoryMap(ringSize, MemoryProtection.Read | MemoryProtection.Write,
            MemoryFlags.Shared | MemoryFlags.Populate, _ringFd, (long)IORING_OFF_SQ_RING);
        if (sqHandle.IsInvalid) throw new MapQueueFailedException(MapQueueFailedException.QueueType.Submission);

        var size = io_uring_sqe.Size;
        if ((p.flags & IORING_SETUP_SQE128) != 0) size += 64;

        sqeHandle = MemoryMap(size * p.sq_entries, MemoryProtection.Read | MemoryProtection.Write,
            MemoryFlags.Shared | MemoryFlags.Populate, _ringFd, (long)IORING_OFF_SQES);
        if (sqeHandle.IsInvalid) throw new MapQueueFailedException(MapQueueFailedException.QueueType.Submission);

        return new SubmissionQueue(this, sqHandle, sqeHandle, ringSize, in p.sq_off, _flags);
    }

    /// <summary>
    /// </summary>
    /// <param name="p"></param>
    /// <param name="ringSize"></param>
    /// <param name="cqHandle"></param>
    /// <returns></returns>
    /// <exception cref="MapQueueFailedException">Throw if <see cref="Linux.LibC.MemMap" /> fails.</exception>
    private CompletionQueue MapCompletionQueue(in io_uring_params p, uint ringSize, out MMapHandle cqHandle)
    {
        if ((p.features & IORING_FEAT_SINGLE_MMAP) != 0)
        {
            cqHandle = _sqMMapHandle;
        }
        else
        {
            cqHandle = MemoryMap(ringSize, MemoryProtection.Read | MemoryProtection.Write,
                MemoryFlags.Shared | MemoryFlags.Populate, _ringFd, (long)IORING_OFF_CQ_RING);
            if (cqHandle.IsInvalid) throw new MapQueueFailedException(MapQueueFailedException.QueueType.Completion);
        }

        return new CompletionQueue(this, cqHandle, ringSize, in p.cq_off, _flags);
    }

    public void SetNotFork()
    {
        _submissionQueue.SetNotFork();
        if (_cqMMapHandle == _sqMMapHandle) return;
        _completionQueue.SetNotFork();
    }

    public int GetEvents()
    {
        var flags = IORING_ENTER_GETEVENTS;
        if (_intFlags.HasFlag(RingInterrupt.RegRing)) flags |= IORING_ENTER_REGISTERED_RING;
        sigset_t sigset = default;
        return io_uring_enter(_enterRingFd, 0, 0, flags, ref sigset);
    }

    public bool TryGetNextSubmission(out Submission.Submission submission)
    {
        return _submissionQueue.TryGetNextSubmission(out submission);
    }

    public int GetSubmissions(Span<Submission.Submission> submissions)
    {
        return _submissionQueue.TryGetNextSubmissions(submissions);
    }

    public void Prepared(in Submission.Submission sqe)
    {
        _submissionQueue.NotifyPrepared(sqe.Index);
    }

    internal bool CqRingNeedsEnter()
    {
        return IsKernelIoPolling || CqRingNeedsFlush();
    }

    internal unsafe bool CqRingNeedsFlush()
    {
        return (*_submissionQueue._kFlags & (IORING_SQ_CQ_OVERFLOW | IORING_SQ_TASKRUN)) != 0;
    }

    public int Submit()
    {
        var result = _submissionQueue.Submit(_enterRingFd);
        ProcessPendingSubmissions();
        return result;
    }

    public int SubmitAndWait(uint waitNr)
    {
        var result = _submissionQueue.SubmitAndWait(_enterRingFd, waitNr);
        ProcessPendingSubmissions();
        return result;
    }

    public bool TryGetCompletion(out Completion.Completion cqe)
    {
        return _completionQueue.TryGetCompletion(out cqe);
    }

    public uint TryGetCompletions(Span<Completion.Completion> completions)
    {
        return _completionQueue.TryGetBatch(completions);
    }

    private void QueueSubmission(Action<Submission.Submission> action)
    {
        _pendingSubmissions.Enqueue(action);
    }

    private void ProcessPendingSubmissions()
    {
        while (_pendingSubmissions.TryPeek(out var action))
            if (TryGetNextSubmission(out var sqe))
            {
                action(sqe);
                _pendingSubmissions.Dequeue();
            }
            else
            {
                break;
            }
    }

    private void Issue(Action<Submission.Submission> action)
    {
        if (TryGetNextSubmission(out var sqe))
        {
            action(sqe);
            return;
        }

        QueueSubmission(action);
    }

    /// <summary>
    ///     Register a buffer group to the ring.
    /// </summary>
    /// <param name="bufferSize">Size of each buffer.</param>
    /// <param name="bufferCount">Number of buffers.</param>
    /// <returns>Group id for the buffer group.</returns>
    public Task<int> RegisterBufferGroupAsync(uint bufferSize, uint bufferCount)
    {
        var id = _lastGroupId++;
        var bufferGroup = new BufferGroup(bufferSize, bufferCount);
        var tcs = new TaskCompletionSource<int>();

        Issue(sqe =>
        {
            unsafe
            {
                sqe.PrepareProvideBuffers(bufferGroup.Base, (int)bufferGroup.BufferSize, (int)bufferGroup.BufferCount,
                    id, 0);
                Prepared(sqe);
                SubmitAndWait(1);
            }

            _bufferGroups.Add(id, bufferGroup);
            tcs.SetResult(id);
        });

        return tcs.Task;
    }

    /// <summary>
    ///     Unregister a buffer group from the ring. If the buffer group is still in use, the operation will fail.
    ///     If the buffer group does not exist, the operation will be ignored.
    /// </summary>
    /// <param name="bufferGroupId">Group id of the buffer group.</param>
    public void UnregisterBufferGroup(int bufferGroupId)
    {
        if (!_bufferGroups.TryGetValue(bufferGroupId, out var bufferGroup)) return;

        _bufferGroups.Remove(bufferGroupId);

        Issue(sqe =>
        {
            sqe.PrepareRemoveBuffers((int)bufferGroup.BufferCount, bufferGroupId);
            Prepared(sqe);
            SubmitAndWait(1);
        });
    }

    #region Basic fields

    private readonly FileDescriptor _ringFd;
    private FileDescriptor _enterRingFd;
    private RingInterrupt _intFlags;
    private readonly RingFeature _features;
    private readonly RingSetup _flags;

    #endregion

    #region Submission Queue

    private readonly SubmissionQueue _submissionQueue;
    private readonly MMapHandle _sqMMapHandle;
    private readonly MMapHandle _sqeMMapHandle;

    #endregion

    #region Completion Queue

    private readonly CompletionQueue _completionQueue;
    private readonly MMapHandle _cqMMapHandle;

    # endregion

    # region Buffer Groups

    private int _lastGroupId;
    private readonly Dictionary<int, BufferGroup> _bufferGroups = new();

    # endregion
}