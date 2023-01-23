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
            _submissionQueue = MapSubmissionQueue(_ringFd, in p, sqSize, _flags, out _sqMMapHandle, out _sqeMMapHandle);
            _completionQueue = MapCompletionQueue(_ringFd, in p, cqSize, _flags, in _sqMMapHandle, out _cqMMapHandle);
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

    private int GetEvents()
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

    internal int Submit(uint submitted, uint waitNr, bool getEvents)
    {
        var cqNeedsEnter = getEvents || waitNr != 0 || CqRingNeedsEnter();
        int ret;

        uint flags = 0;
        if (SqRingNeedsEnter(submitted, ref flags) || cqNeedsEnter)
        {
            if (cqNeedsEnter)
                flags |= IORING_ENTER_GETEVENTS;
            if (_intFlags.HasFlag(RingInterrupt.RegRing))
                flags |= IORING_ENTER_REGISTERED_RING;

            var sigset = default(sigset_t);
            ret = io_uring_enter(_enterRingFd, submitted, waitNr, flags, ref sigset);
        }
        else
        {
            ret = (int)submitted;
        }

        return ret;
    }

    private bool SqRingNeedsEnter(uint submit, ref uint flags)
    {
        if (submit == 0)
            return false;
        if (IsKernelSubmissionQueuePolling)
            return true;

        Thread.MemoryBarrier();
        unsafe
        {
            if ((*_submissionQueue._kFlags & IORING_SQ_NEED_WAKEUP) == 0) return false;
            flags |= IORING_ENTER_SQ_WAKEUP;
            return true;
        }
    }

    private bool CqRingNeedsEnter()
    {
        return IsKernelIoPolling || CqRingNeedsFlush();
    }

    private bool CqRingNeedsFlush()
    {
        unsafe
        {
            return (*_submissionQueue._kFlags & (IORING_SQ_CQ_OVERFLOW | IORING_SQ_TASKRUN)) != 0;
        }
    }

    public int Submit()
    {
        return Submit(_submissionQueue.Flush(), 0, false);
    }

    public bool TryGetCompletion(out Completion.Completion cqe)
    {
        return _completionQueue.TryGetCompletion(out cqe);
    }
}