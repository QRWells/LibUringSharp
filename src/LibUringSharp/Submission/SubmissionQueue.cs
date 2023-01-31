using System.Runtime.CompilerServices;
using LibUringSharp.Enums;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp.Submission;

public sealed unsafe class SubmissionQueue
{
    private readonly Ring _parent;
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
    private int[] _sqeState;
    private const int SqeStateFree = 0;
    private const int SqeStateReserved = 1;
    private const int SqeStatePrepared = 2;
    private const int SqeStateSubmitted = 3;

    public SubmissionQueue(Ring parent, MMapHandle sqPtr, MMapHandle sqePtr, uint ringSize, in io_sqring_offsets offsets,
        RingSetup flags)
    {
        _parent = parent;
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

        _sqeState = new int[_ringEntries];
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
        uint head;
        var next = unchecked(_sqeTail + 1);
        var shift = 0;

        if (_flags.HasFlag(RingSetup.Sqe128))
            shift = 1;
        if (!_flags.HasFlag(RingSetup.KernelSubmissionQueuePolling))
            head = *_kHead;
        else
            head = Volatile.Read(ref *_kHead);

        if (next - head <= _ringEntries)
        {
            var idx = (_sqeTail & _ringMask) << shift;
            var internalSqe = &_sqes[idx];
            Unsafe.InitBlockUnaligned(internalSqe, 0, (uint)io_uring_sqe.Size);
            _sqeTail = next;
            sqe = new Submission(this, internalSqe, idx);
            Volatile.Write(ref _sqeState[idx], SqeStateReserved);
            return true;
        }

        sqe = default;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void NotifyPrepared(uint idx)
    {
        Volatile.Write(ref _sqeState[idx], SqeStatePrepared);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool CqRingNeedsEnter()
    {
        return _flags.HasFlag(RingSetup.KernelIoPolling) || CqRingNeedsFlush();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private unsafe bool CqRingNeedsFlush()
    {
        return (*_kFlags & (IORING_SQ_CQ_OVERFLOW | IORING_SQ_TASKRUN)) != 0;
    }

    private bool NeedsEnter(uint submit, ref uint flags)
    {
        if (submit == 0)
            return false;
        if (!_flags.HasFlag(RingSetup.KernelSubmissionQueuePolling))
            return true;

        Thread.MemoryBarrier();

        if ((*_kFlags & IORING_SQ_NEED_WAKEUP) == 0) return false;
        flags |= IORING_ENTER_SQ_WAKEUP;
        return true;
    }

    private int SubmitInternal(FileDescriptor enterRingFd, uint submitted, uint waitNr, bool getEvents)
    {
        var cqNeedsEnter = getEvents || waitNr != 0 || CqRingNeedsEnter();
        int ret;

        uint flags = 0;
        if (NeedsEnter(submitted, ref flags) || cqNeedsEnter)
        {
            if (cqNeedsEnter)
                flags |= IORING_ENTER_GETEVENTS;
            if (_parent.IsIntteruptRegistered)
                flags |= IORING_ENTER_REGISTERED_RING;

            var sigset = default(sigset_t);
            ret = io_uring_enter(enterRingFd, submitted, waitNr, flags, ref sigset);
        }
        else
        {
            ret = (int)submitted;
        }

        Reset(ret);

        return ret;
    }

    private void Reset(int submitted)
    {
        var head = _sqeHead;

        while (submitted-- != 0)
        {
            var idx = head & _ringMask;
            _sqeState[idx] = SqeStateFree;
            head = unchecked(head + 1);
        }

        _sqeHead = head;
    }

    internal int Submit(FileDescriptor enterRingFd)
    {
        return SubmitInternal(enterRingFd, Flush(), 0, false);
    }

    internal int SubmitAndWait(FileDescriptor enterRingFd, uint waitNr)
    {
        return SubmitInternal(enterRingFd, Flush(), waitNr, false);
    }

    internal uint Flush()
    {
        var tail = _sqeTail;

        if (_sqeHead == tail) return tail - *_kHead;

        var head = _sqeHead;

        do
        {
            var idx = head & _ringMask;
            if (Volatile.Read(ref _sqeState[idx]) < SqeStatePrepared)
                break;
            Volatile.Write(ref _sqeState[idx], SqeStateSubmitted);
            head++;
        } while (head < tail);

        _sqeHead = head;

        // Ensure kernel sees the SQE updates before the tail update.
        if (!_flags.HasFlag(RingSetup.KernelSubmissionQueuePolling))
            *_kTail = tail;
        else
            Volatile.Write(ref *_kTail, tail);
        return tail - *_kHead;
    }
}