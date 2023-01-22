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

    private readonly RingSetup _flags;

    private readonly FileDescriptor _ringFd;
    private readonly MMapHandle _sqeMMapHandle;
    private readonly MMapHandle _sqMMapHandle;
    private readonly SubmissionQueue _submissionQueue;
    private FileDescriptor _enterRingFd;
    private RingInterrupt _intFlags;

    public Ring(uint entries, uint flags)
    {
        io_uring_params p = default;
        p.flags = flags;

        _ringFd = io_uring_setup(entries, ref p);

        if (_ringFd.IsInvalid)
            throw new Exception("io_uring_setup failed");

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

    private static (uint, uint) ComputeRingSize(in io_uring_params p)
    {
        var size = (uint)io_uring_cqe.Size;
        if ((p.features & IORING_SETUP_CQE32) != 0) size += io_uring_cqe.Size;

        var sqSize = p.sq_off.array + p.sq_entries * sizeof(uint);
        var cqSize = p.cq_off.cqes + p.cq_entries * size;

        if ((p.features & IORING_FEAT_SINGLE_MMAP) == 0) return (sqSize, cqSize);

        if (cqSize > sqSize)
            sqSize = cqSize;
        cqSize = sqSize;

        return (sqSize, cqSize);
    }

    private static SubmissionQueue MapSubmissionQueue(
        in FileDescriptor ringFd, in io_uring_params p, uint ringSize, RingSetup flags,
        out MMapHandle sqHandle,
        out MMapHandle sqeHandle)
    {
        sqHandle = MemoryMap(ringSize, MemoryProtection.Read | MemoryProtection.Write,
            MemoryFlags.Shared | MemoryFlags.Populate, ringFd, (long)IORING_OFF_SQ_RING);
        if (sqHandle.IsInvalid) throw new MapQueueFailedException(MapQueueFailedException.QueueType.Submission);

        var size = io_uring_sqe.Size;
        if ((p.flags & IORING_SETUP_SQE128) != 0) size += 64;

        sqeHandle = MemoryMap(size * p.sq_entries, MemoryProtection.Read | MemoryProtection.Write,
            MemoryFlags.Shared | MemoryFlags.Populate, ringFd, (long)IORING_OFF_SQES);
        if (sqeHandle.IsInvalid) throw new MapQueueFailedException(MapQueueFailedException.QueueType.Submission);

        return new SubmissionQueue(sqHandle, sqeHandle, ringSize, in p.sq_off, flags);
    }

    private static CompletionQueue MapCompletionQueue(
        in FileDescriptor ringFd, in io_uring_params p, uint ringSize, RingSetup flags,
        in MMapHandle sqHandle,
        out MMapHandle cqHandle)
    {
        if ((p.features & IORING_FEAT_SINGLE_MMAP) != 0)
        {
            cqHandle = sqHandle;
        }
        else
        {
            cqHandle = MemoryMap(ringSize, MemoryProtection.Read | MemoryProtection.Write,
                MemoryFlags.Shared | MemoryFlags.Populate, ringFd, (long)IORING_OFF_CQ_RING);
            if (cqHandle.IsInvalid) throw new MapQueueFailedException(MapQueueFailedException.QueueType.Completion);
        }

        return new CompletionQueue(cqHandle, ringSize, in p.cq_off, flags);
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
}