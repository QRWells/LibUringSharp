using System.Runtime.CompilerServices;
using QRWells.LibUringSharp.Enums;
using QRWells.LibUringSharp.Linux.Handles;
using static QRWells.LibUringSharp.Linux.LibC;

namespace QRWells.LibUringSharp.Submission;

public sealed unsafe class SubmissionQueue
{
    private readonly uint* _array;
    private readonly RingSetup _flags;
    private readonly uint* _kHead;
    private readonly uint* _kTail;
    private readonly Ring _parent;
    private readonly uint _ringEntries;
    private readonly uint _ringMask;
    private readonly MMapHandle _ringPtr;
    private readonly ulong _ringSize;
    private readonly io_uring_sqe* _sqes;
    private uint* _kDropped;
    internal uint* _kFlags;
    private uint _sqeHead;
    private uint _sqeTail;

    public SubmissionQueue(Ring parent, MMapHandle sqPtr, MMapHandle sqePtr, uint ringSize,
        in io_sqring_offsets offsets,
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

    private int Shift => _flags.HasFlag(RingSetup.Sqe128) ? 1 : 0;
    private bool IsSqPolling => _flags.HasFlag(RingSetup.KernelSubmissionQueuePolling);

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

    internal bool TryGetNextSubmission(out Submission submission)
    {
        uint head;
        var next = unchecked(_sqeTail + 1);

        if (!IsSqPolling)
            head = *_kHead;
        else
            head = Volatile.Read(ref *_kHead);

        // No more free submissions
        if (next - head > _ringEntries)
        {
            submission = default;
            return false;
        }

        var idx = (_sqeTail & _ringMask) << Shift;

        if (_sqeState[idx] > SqeStateFree)
        {
            submission = default;
            return false;
        }

        var internalSqe = &_sqes[idx];
        Unsafe.InitBlockUnaligned(internalSqe, 0, (uint)io_uring_sqe.Size);

        _sqeTail = next;
        Volatile.Write(ref _sqeState[idx], SqeStateReserved);
        submission = new Submission(internalSqe, idx);

        return true;
    }

    internal int TryGetNextSubmissions(Span<Submission> submissions)
    {
        if (submissions.Length == 0) return 0;

        uint head;
        var tail = _sqeTail;
        var next = unchecked(_sqeTail + submissions.Length);

        if (!IsSqPolling)
            head = *_kHead;
        else
            head = Volatile.Read(ref *_kHead);

        // No more free submissions
        if (next - head > _ringEntries) return 0;

        _sqeTail = (uint)next;

        var count = 0;

        for (var i = 0; i < submissions.Length; ++i)
        {
            var idx = (tail & _ringMask) << Shift;
            tail = unchecked(tail + 1);

            if (_sqeState[idx] > SqeStateFree) break;

            var internalSqe = &_sqes[idx];
            Unsafe.InitBlockUnaligned(internalSqe, 0, (uint)io_uring_sqe.Size);
            Volatile.Write(ref _sqeState[idx], SqeStateReserved);
            submissions[i] = new Submission(internalSqe, idx);
            ++count;
        }

        return count;
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
    private bool CqRingNeedsFlush()
    {
        return (*_kFlags & (IORING_SQ_CQ_OVERFLOW | IORING_SQ_TASKRUN)) != 0;
    }

    private bool NeedsEnter(uint submit, ref uint flags)
    {
        if (submit == 0)
            return false;
        if (!IsSqPolling)
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
            if (_parent.IsInterruptRegistered)
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
            var idx = (head & _ringMask) << Shift;
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

    private uint Flush()
    {
        var head = _sqeHead;
        var tail = _sqeTail;
        if (head == tail) return 0;

        var kTail = Volatile.Read(ref *_kTail);

        while (head < tail)
        {
            var idx = (head & _ringMask) << Shift;
            if (_sqeState[idx] < SqeStatePrepared)
                break;

            _sqeState[idx] = SqeStateSubmitted;
            // Increment kernel tail for each consecutive prepare sqe
            kTail = unchecked(kTail + 1);
            head = unchecked(head + 1);
        }

        // Ensure kernel sees the SQE updates before the tail update.
        if (!IsSqPolling)
            *_kTail = kTail;
        else
            Volatile.Write(ref *_kTail, kTail);
        return kTail - *_kHead;
    }

    # region Submission states

    private const int SqeStateFree = 0;
    private const int SqeStateReserved = 1;
    private const int SqeStatePrepared = 2;
    private const int SqeStateSubmitted = 3;
    private readonly int[] _sqeState;

    # endregion
}