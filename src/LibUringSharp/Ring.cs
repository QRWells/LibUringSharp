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
    private readonly CompletionQueue _completionQueue;

    private readonly MMapHandle _cqMMapHandle;
    private readonly RingFeature _features;

    private readonly RingSetup _flags = RingSetup.None;
    private readonly FileDescriptor _ringFd;
    private readonly MMapHandle _sqeMMapHandle;
    private readonly MMapHandle _sqMMapHandle;
    private readonly SubmissionQueue _submissionQueue;
    private FileDescriptor _enterRingFd;
    private RingInterrupt _intFlags;

    /// <summary>
    ///     Constructs a new <see cref="Ring" /> with the given number of entries and flags
    /// </summary>
    /// <param name="entries">Entries in the ring</param>
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

    /// <summary>
    ///     Map the submission queue and submission queue entries
    /// </summary>
    /// <param name="ringFd"><see cref="FileDescriptor" /> of the <see cref="Ring" /></param>
    /// <param name="p">parameters of the <see cref="Ring" /></param>
    /// <param name="ringSize">size of the submission queue ring</param>
    /// <param name="flags">setup flags of the <see cref="Ring" /></param>
    /// <param name="sqHandle">out <see cref="MMapHandle" /> of the submission queue</param>
    /// <param name="sqeHandle">out <see cref="MMapHandle" /> of the submission queue entries</param>
    /// <returns></returns>
    /// <exception cref="MapQueueFailedException">Throw if <see cref="LibC.MemMap" /> fails.</exception>
    private SubmissionQueue MapSubmissionQueue(in io_uring_params p, uint ringSize, out MMapHandle sqHandle, out MMapHandle sqeHandle)
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
    /// <param name="ringFd"></param>
    /// <param name="p"></param>
    /// <param name="ringSize"></param>
    /// <param name="flags"></param>
    /// <param name="sqHandle"></param>
    /// <param name="cqHandle"></param>
    /// <returns></returns>
    /// <exception cref="MapQueueFailedException">Throw if <see cref="LibC.MemMap" /> fails.</exception>
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

    public bool IsKernelIoPolling => _flags.HasFlag(RingSetup.KernelIoPolling);
    public bool IsKernelSubmissionQueuePolling => _flags.HasFlag(RingSetup.KernelSubmissionQueuePolling);

    internal bool IsIntteruptRegistered => _intFlags.HasFlag(RingInterrupt.RegRing);

    public void Dispose()
    {
        _sqMMapHandle.Dispose();
        _sqeMMapHandle.Dispose();
        _cqMMapHandle.Dispose();
        if (_intFlags.HasFlag(RingInterrupt.RegRing))
            RegisterRingFd();
        _ringFd.Dispose();
        _enterRingFd.Dispose();
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

    private unsafe void RegisterRingFd()
    {
        var up = new io_uring_rsrc_update
        {
            offset = _enterRingFd
        };

        var ret = io_uring_register(_ringFd, IORING_UNREGISTER_RING_FDS, &up, 1);

        if (ret != 1) return;
        _enterRingFd = _ringFd;
        _intFlags &= ~RingInterrupt.RegRing;
    }

    internal unsafe int RegisterProbe(io_uring_probe* p, uint nrOps)
    {
        return io_uring_register(_ringFd, IORING_REGISTER_PROBE, p, nrOps);
    }

    public bool TryGetNextSqe(out Submission.Submission sqe)
    {
        return _submissionQueue.TryGetNextSqe(out sqe);
    }

    internal bool CqRingNeedsEnter()
    {
        return IsKernelIoPolling || CqRingNeedsFlush();
    }

    internal bool CqRingNeedsFlush()
    {
        unsafe
        {
            return (*_submissionQueue._kFlags & (IORING_SQ_CQ_OVERFLOW | IORING_SQ_TASKRUN)) != 0;
        }
    }

    public int Submit()
    {
        return _submissionQueue.Submit(_enterRingFd);
    }

    public int SubmitAndWait(uint waitNr)
    {
        return _submissionQueue.SubmitAndWait(_enterRingFd, waitNr);
    }

    public bool TryGetCompletion(out Completion.Completion cqe)
    {
        return _completionQueue.TryGetCompletion(out cqe);
    }

    public uint TryGetBatch(Span<Completion.Completion> completions)
    {
        return _completionQueue.TryGetBatch(completions);
    }
}