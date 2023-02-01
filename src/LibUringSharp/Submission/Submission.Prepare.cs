using LibUringSharp.Enums;
using static Linux.LibC;

namespace LibUringSharp.Submission;

public unsafe readonly partial struct Submission
{
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