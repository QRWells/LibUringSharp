using LibUringSharp.Enums;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp.Submission;

public sealed unsafe class SubmissionQueue
{
    private readonly RingSetup _flags;

    /// <summary>
    ///     A pointer to uint
    /// </summary>
    private readonly MMapHandle _ringPtr;

    private readonly ulong _ringSize;

    /// <summary>
    ///     A pointer to uint
    /// </summary>
    private readonly uint* _array;

    /// <summary>
    ///     A pointer to io_uring_sqe
    /// </summary>
    private readonly io_uring_sqe* _sqes;

    /// <summary>
    ///     A pointer to uint
    /// </summary>
    private uint* kdropped;

    /// <summary>
    ///     A pointer to uint
    /// </summary>
    private uint* kflags;

    /// <summary>
    ///     A pointer to uint
    /// </summary>
    private uint* khead;

    /// <summary>
    ///     A pointer to uint
    /// </summary>
    private uint* ktail;

    private readonly uint ring_entries;
    private uint ring_mask;

    private uint sqe_head;
    private uint sqe_tail;

    public SubmissionQueue(MMapHandle sqPtr, MMapHandle sqePtr, uint ringSize, in io_sqring_offsets offsets,
        RingSetup flags)
    {
        _ringPtr = sqPtr;
        _ringSize = ringSize;

        khead = (uint*)(sqPtr.Address + offsets.head);
        ktail = (uint*)(sqPtr.Address + offsets.tail);
        ring_mask = *(uint*)(sqPtr.Address + offsets.ring_mask);
        ring_entries = *(uint*)(sqPtr.Address + offsets.ring_entries);
        kflags = (uint*)(sqPtr.Address + offsets.flags);
        kdropped = (uint*)(sqPtr.Address + offsets.dropped);
        _array = (uint*)(sqPtr.Address + offsets.array);

        _sqes = (io_uring_sqe*)sqePtr.Address;
        _flags = flags;
        // Directly map SQ slots to SQEs
        for (var i = 0u; i < ring_entries; ++i) _array[i] = i;
    }

    internal void SetNotFork()
    {
        if (_ringPtr.Address == nint.Zero || _sqes != (void*)nint.Zero)
            throw new InvalidOperationException("Ring is not initialized");

        var len = io_uring_sqe.Size;
        if (_flags.HasFlag(RingSetup.Sqe128)) len += 64;
        len *= ring_entries;
        var ret = MemAdvise(new nint(_sqes), len, MADV_DONTFORK);
        if (ret < 0) throw new Exception("MemAdvise failed");

        len = _ringSize;
        ret = MemAdvise(_ringPtr.Address, len, MADV_DONTFORK);
        if (ret < 0) throw new Exception("MemAdvise failed");
    }
}