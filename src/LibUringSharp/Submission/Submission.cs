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

    public SubmissionOption Option
    {
        get => (SubmissionOption)_sqe->flags;
        set => _sqe->flags = (byte)value;
    }
}

[Flags]
public enum SubmissionOption : byte
{
    /// <summary>
    /// use fixed fileset
    /// </summary>
    FixedFile = 1 << 1,

    /// <summary>
    /// issue after inflight IO
    /// </summary>
    IoDrain = 1 << 2,

    /// <summary>
    /// links next sqe
    /// </summary>
    IoLink = 1 << 3,

    /// <summary>
    /// like LINK, but stronger
    /// </summary>
    IoHardLink = 1 << 4,

    /// <summary>
    /// always go async
    /// </summary>
    Async = 1 << 5,

    /// <summary>
    /// select buffer from sqe->buf_group
    /// </summary>
    BufferSelect = 1 << 6,

    /// <summary>
    /// don't post CQE if request succeeded
    /// </summary>
    CqeSkipSuccess = 1 << 7
}