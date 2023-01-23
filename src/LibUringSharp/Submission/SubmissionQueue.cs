using System.Runtime.CompilerServices;
using LibUringSharp.Enums;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp.Submission;

public sealed unsafe class SubmissionQueue
{
    private readonly uint* _array;
    private readonly RingSetup _flags;
    private readonly uint _ringEntries;
    private readonly MMapHandle _ringPtr;
    private readonly ulong _ringSize;
    private readonly io_uring_sqe* _sqes;
    private uint* _kDropped;
    internal uint* _kFlags;
    private readonly uint* _kHead;
    private readonly uint* _kTail;
    private readonly uint _ringMask;
    private uint _sqeHead;
    private uint _sqeTail;

    public SubmissionQueue(MMapHandle sqPtr, MMapHandle sqePtr, uint ringSize, in io_sqring_offsets offsets,
        RingSetup flags)
    {
        _ringPtr = sqPtr;
        _ringSize = ringSize;

        _kHead = (uint*)(sqPtr.Address + offsets.head);
        _kTail = (uint*)(sqPtr.Address + offsets.tail);
        _ringMask = *(uint*)(sqPtr.Address + offsets.ring_mask);
        _ringEntries = *(uint*)(sqPtr.Address + offsets.ring_entries);
        _kFlags = (uint*)(sqPtr.Address + offsets.flags);
        _kDropped = (uint*)(sqPtr.Address + offsets.dropped);
        _array = (uint*)(sqPtr.Address + offsets.array);

        _sqes = (io_uring_sqe*)sqePtr.Address;
        _flags = flags;
        // Directly map SQ slots to SQEs
        for (var i = 0u; i < _ringEntries; ++i) _array[i] = i;
    }

    internal void SetNotFork()
    {
        if (_ringPtr.Address == nint.Zero || _sqes != (void*)nint.Zero)
            throw new InvalidOperationException("Ring is not initialized");

        var len = io_uring_sqe.Size;
        if (_flags.HasFlag(RingSetup.Sqe128)) len += 64;
        len *= _ringEntries;
        var ret = MemAdvise(new nint(_sqes), len, MADV_DONTFORK);
        if (ret < 0) throw new Exception("MemAdvise failed");

        len = _ringSize;
        ret = MemAdvise(_ringPtr.Address, len, MADV_DONTFORK);
        if (ret < 0) throw new Exception("MemAdvise failed");
    }

    internal bool TryGetNextSqe(out Submission sqe)
    {
        uint head, next = _sqeTail + 1;
        var shift = 0;

        if (_flags.HasFlag(RingSetup.Sqe128))
            shift = 1;
        if (!_flags.HasFlag(RingSetup.KernelSubmissionQueuePolling))
            head = *_kHead;
        else
            head = Volatile.Read(ref *_kHead);

        if (next - head <= _ringEntries)
        {
            var internalSqe = &_sqes[(_sqeTail & _ringMask) << shift];
            Unsafe.InitBlockUnaligned(internalSqe, 0, (uint)io_uring_sqe.Size);
            _sqeTail = next;
            sqe = new Submission(internalSqe);
            return true;
        }

        sqe = default;
        return false;
    }

    internal uint Flush()
    {
        var tail = _sqeTail;

        if (_sqeHead == tail) return tail - *_kHead;
        _sqeHead = tail;
        // Ensure kernel sees the SQE updates before the tail update.
        if (!_flags.HasFlag(RingSetup.KernelSubmissionQueuePolling))
            *_kTail = tail;
        else
            Volatile.Write(ref *_kTail, tail);
        return tail - *_kHead;
    }
}