using System.Runtime.CompilerServices;
using QRWells.LibUringSharp.Enums;
using QRWells.LibUringSharp.Linux.Handles;
using static QRWells.LibUringSharp.Linux.LibC;

namespace QRWells.LibUringSharp.Completion;

public sealed unsafe class CompletionQueue
{
    private readonly io_uring_cqe* _cqes;
    private readonly RingSetup _flags;
    private readonly uint* _kHead;
    private readonly uint* _kTail;
    private readonly Ring _parent;
    private readonly uint _ringMask;
    private readonly MMapHandle _ringPtr;

    private readonly ulong _ringSize;

    private uint* _kFlags;
    private uint* _kOverflow;
    private uint _ringEntries;

    public CompletionQueue(Ring parent, MMapHandle cqPtr, uint ringSize, in io_cqring_offsets offsets, RingSetup flags)
    {
        _parent = parent;
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

    private int Shift => _flags.HasFlag(RingSetup.Cqe32) ? 1 : 0;

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

        // No Completion Queue Event available
        if (head == *_kTail)
        {
            cqe = default;
            return false;
        }

        var internalCqe = &_cqes[(head & _ringMask) << Shift];

        cqe = new Completion(internalCqe->res, internalCqe->user_data, internalCqe->flags);

        Volatile.Write(ref *_kHead, head + 1);
        return true;
    }

    public void IgnoreCompletions(int count = 1)
    {
        var head = Volatile.Read(ref *_kHead);

        // No Completion could be ignored
        if (head == *_kTail) return;

        Volatile.Write(ref *_kHead, head + (uint)count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private uint Ready()
    {
        return Volatile.Read(ref *_kHead) - *_kTail;
    }

    internal uint TryGetBatch(Span<Completion> completions)
    {
        var overflowChecked = false;

        again:
        var ready = Ready();
        if (ready != 0)
        {
            var head = *_kHead;
            var i = 0;

            var count = Math.Min(ready, (uint)completions.Length);
            var last = head + count;
            for (; head != last; head++, i++)
            {
                var internalCqe = &_cqes[(head & _ringMask) << Shift];
                completions[i] = new Completion(internalCqe->res, internalCqe->user_data, internalCqe->flags);
            }

            return count;
        }

        if (overflowChecked)
            return 0;

        if (!_parent.CqRingNeedsFlush()) return 0;
        _parent.GetEvents();
        overflowChecked = true;
        goto again;
    }
}