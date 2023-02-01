using static Linux.LibC;

namespace LibUringSharp.Submission;

public readonly unsafe partial struct Submission
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
}