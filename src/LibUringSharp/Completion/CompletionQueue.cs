using LibUringSharp.Enums;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp.Completion;

public sealed unsafe class CompletionQueue
{
    private readonly MMapHandle _ringPtr;

    private readonly ulong _ringSize;
    private RingSetup _flags;

    /// <summary>
    ///     A pointer to the underlying <see cref="io_uring_cqe" /> structure.
    /// </summary>
    private io_uring_cqe* cqes;

    private uint* kflags;
    private uint* khead;
    private uint* koverflow;

    private uint* ktail;
    private uint ring_entries;

    private uint ring_mask;

    public CompletionQueue(MMapHandle cqPtr, uint ringSize, in io_cqring_offsets offsets, RingSetup flags)
    {
        _ringPtr = cqPtr;
        _flags = flags;
        _ringSize = ringSize;

        khead = (uint*)(cqPtr.Address + offsets.head);
        ktail = (uint*)(cqPtr.Address + offsets.tail);
        ring_mask = *(uint*)(cqPtr.Address + offsets.ring_mask);
        ring_entries = *(uint*)(cqPtr.Address + offsets.ring_entries);
        koverflow = (uint*)(cqPtr.Address + offsets.overflow);
        cqes = (io_uring_cqe*)(cqPtr.Address + offsets.cqes);
        if (offsets.flags != 0)
            kflags = (uint*)(cqPtr.Address + offsets.flags);
    }

    internal void SetNotFork()
    {
        if (_ringPtr.Address == nint.Zero)
            throw new InvalidOperationException("Ring is not initialized");

        var len = _ringSize;
        var ret = MemAdvise(_ringPtr.Address, len, MADV_DONTFORK);
        if (ret < 0) throw new Exception("MemAdvise failed");
    }
}