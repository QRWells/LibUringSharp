using LibUringSharp.Enums;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp.Completion;

public sealed unsafe class CompletionQueue
{
    private readonly MMapHandle _ringPtr;

    private readonly ulong _ringSize;

    private readonly io_uring_cqe* _cqes;
    private RingSetup _flags;

    private uint* _kFlags;
    internal uint* _kHead;
    private uint* _kOverflow;
    private readonly uint* _kTail;
    private uint _ringEntries;
    private readonly uint _ringMask;

    public CompletionQueue(MMapHandle cqPtr, uint ringSize, in io_cqring_offsets offsets, RingSetup flags)
    {
        _ringPtr = cqPtr;
        _flags = flags;
        _ringSize = ringSize;

        _kHead = (uint*)(cqPtr.Address + offsets.head);
        _kTail = (uint*)(cqPtr.Address + offsets.tail);
        _ringMask = *(uint*)(cqPtr.Address + offsets.ring_mask);
        _ringEntries = *(uint*)(cqPtr.Address + offsets.ring_entries);
        _kOverflow = (uint*)(cqPtr.Address + offsets.overflow);
        _cqes = (io_uring_cqe*)(cqPtr.Address + offsets.cqes);
        if (offsets.flags != 0)
            _kFlags = (uint*)(cqPtr.Address + offsets.flags);
    }

    internal void SetNotFork()
    {
        if (_ringPtr.Address == nint.Zero)
            throw new InvalidOperationException("Ring is not initialized");

        var ret = MemAdvise(_ringPtr.Address, _ringSize, MADV_DONTFORK);
        if (ret < 0) throw new Exception("MemAdvise failed");
    }

    public bool TryGetCompletion(out Completion cqe)
    {
        var head = Volatile.Read(ref *_kHead);

        if (head == *_kTail)
        {
            // No Completion Queue Event available
            cqe = default;
            return false;
        }

        var index = head & _ringMask;
        var internalCqe = &_cqes[index];

        cqe = new Completion(internalCqe->res, internalCqe->user_data, internalCqe->flags);

        Volatile.Write(ref *_kHead, head + 1);
        return true;
    }
}