using LibUringSharp.Enums;
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

    private static unsafe int IncreaseResourceLimitFile(uint nr)
    {
        rlimit rLimit;

        var ret = GetRLimit(ResourceLimit.Files, &rLimit);
        if (ret < 0)
            return ret;

        if (rLimit.rlim_cur >= nr) return 0;
        rLimit.rlim_cur += nr;
        SetRLimit(ResourceLimit.Files, &rLimit);

        return 0;
    }
}