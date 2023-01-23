using LibUringSharp.Completion;
using LibUringSharp.Enums;
using LibUringSharp.Exceptions;
using LibUringSharp.Submission;
using Linux;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp;

public sealed partial class Ring
{
    private const int KernMaxEntries = 32768;
    private const int KernMaxCqEntries = 2 * KernMaxEntries;
    private const int KRingSize = 320;

    public static ulong GetMLockSize(uint entries, uint flags)
    {
        var p = new io_uring_params
        {
            flags = flags
        };

        using var ring = new Ring(entries);
        if (ring._features.HasFlag(RingFeature.NativeWorkers)) return 0;

        entries = entries switch
        {
            0 => throw new ArgumentException("entries must be greater than 0", nameof(entries)),
            > KernMaxEntries when (p.flags & IORING_SETUP_CLAMP) != 0 => throw new ArgumentException(
                "entries must be less than or equal to 32768", nameof(entries)),
            > KernMaxEntries => KernMaxEntries,
            _ => entries
        };

        entries = Util.RoundUpPow2(entries);

        uint cqEntries;

        if ((p.flags & IORING_SETUP_CQSIZE) != 0)
        {
            if (p.cq_entries != 0)
                throw new ArgumentOutOfRangeException(nameof(p.cq_entries), "cq_entries must be 0");
            cqEntries = p.cq_entries;
            if (cqEntries > KernMaxCqEntries)
            {
                if ((p.flags & IORING_SETUP_CLAMP) == 0)
                    throw new ArgumentOutOfRangeException(nameof(p.cq_entries),
                        "cq_entries must be less than or equal to 65536");
                cqEntries = KernMaxCqEntries;
            }

            if (cqEntries < entries)
                throw new ArgumentOutOfRangeException(nameof(p.cq_entries),
                    "cq_entries must be greater than or equal to entries");
        }
        else
        {
            cqEntries = 2 * entries;
        }

        return GetRingSize(p, entries, cqEntries, PageSize);
    }

    private static ulong GetRingSize(in io_uring_params p, uint entries, uint cqEntries, uint pageSize)
    {
        ulong cqSize = io_uring_cqe.Size;
        if ((p.flags & IORING_SETUP_CQE32) != 0)
            cqSize += io_uring_cqe.Size;
        cqSize *= cqEntries;
        cqSize += KRingSize;
        cqSize = (cqSize + 63) & ~63UL;
        var pages = 1ul << (int)Util.NPages(cqSize, pageSize);

        var sqSize = io_uring_sqe.Size;
        if ((p.flags & IORING_SETUP_SQE128) != 0)
            sqSize += 64;
        sqSize *= entries;
        pages += 1ul << (int)Util.NPages(sqSize, pageSize);
        return pages * pageSize;
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
}