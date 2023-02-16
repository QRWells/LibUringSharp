using static QRWells.LibUringSharp.Linux.LibC;

namespace QRWells.LibUringSharp.Submission;

public readonly unsafe partial struct Submission
{
    private readonly io_uring_sqe* _sqe;
    internal uint Index { get; }

    internal Submission(io_uring_sqe* sqe, uint index)
    {
        _sqe = sqe;
        Index = index;
    }

    public SubmissionOption Option
    {
        get => (SubmissionOption)_sqe->flags;
        set => _sqe->flags = (byte)value;
    }

    public void SetSelectBufferGroup(ushort group)
    {
        _sqe->buf_group = group;
        Option |= SubmissionOption.BufferSelect;
    }

    public ulong UserData
    {
        set => _sqe->user_data = value;
    }
}

[Flags]
public enum SubmissionOption : byte
{
    None = 0,

    /// <summary>
    ///     use fixed fileset
    /// </summary>
    FixedFile = 1 << 0,

    /// <summary>
    ///     issue after inflight IO
    /// </summary>
    IoDrain = 1 << 1,

    /// <summary>
    ///     links next sqe
    /// </summary>
    IoLink = 1 << 2,

    /// <summary>
    ///     like LINK, but stronger
    /// </summary>
    IoHardLink = 1 << 3,

    /// <summary>
    ///     always go async
    /// </summary>
    Async = 1 << 4,

    /// <summary>
    ///     select buffer from sqe->buf_group
    /// </summary>
    BufferSelect = 1 << 5,

    /// <summary>
    ///     don't post CQE if request succeeded
    /// </summary>
    CqeSkipSuccess = 1 << 6
}