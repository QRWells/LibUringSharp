using LibUringSharp.Enums;
using static Linux.LibC;

namespace LibUringSharp.Submission;

public readonly unsafe struct Submission
{
    private readonly io_uring_sqe* _sqe;
    private readonly uint _index;
    private readonly SubmissionQueue _queue;

    internal Submission(SubmissionQueue sq, io_uring_sqe* sqe, uint index)
    {
        _queue = sq;
        _sqe = sqe;
        _index = index;
    }

    public void PrepareNop(ulong userData = 0, SqeOption options = SqeOption.None, ushort personality = 0)
    {
        unchecked
        {
            _sqe->opcode = (byte)IoUringOp.Nop;
            _sqe->flags = (byte)options;
            _sqe->fd = -1;
            _sqe->user_data = userData;
            _sqe->personality = personality;
        }

        _queue.NotifyPrepared(_index);
    }
}