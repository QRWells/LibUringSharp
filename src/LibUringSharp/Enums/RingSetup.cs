namespace LibUringSharp.Enums;

[Flags]
public enum RingSetup : uint
{
    /// <summary>
    ///     io_context is polled
    /// </summary>
    IoPoll = 1 << 0,

    /// <summary>
    ///     SQ poll thread
    /// </summary>
    SqPoll = 1 << 1,

    /// <summary>
    ///     sq_thread_cpu is valid
    /// </summary>
    SqAff = 1 << 2,

    /// <summary>
    ///     app defines CQ size
    /// </summary>
    CqSize = 1 << 3,

    /// <summary>
    ///     clamp SQ/CQ ring sizes
    /// </summary>
    Clamp = 1 << 4,

    /// <summary>
    ///     attach to existing wq
    /// </summary>
    AttachWq = 1 << 5,

    /// <summary>
    ///     start with ring disabled
    /// </summary>
    RDisabled = 1 << 6,

    /// <summary>
    ///     continue submit on error
    /// </summary>
    SubmitAll = 1 << 7,

    /// <summary>
    ///     Cooperative task running. When requests complete, they often require
    ///     forcing the submitter to transition to the kernel to complete. If this
    ///     flag is set, work will be done when the task transitions anyway, rather
    ///     than force an inter-processor interrupt reschedule. This avoids interrupting
    ///     a task running in userspace, and saves an IPI.
    /// </summary>
    CoopTaskRun = 1 << 8,

    /// <summary>
    ///     If <see cref="CoopTaskRun" /> is set, get notified if task work is available for
    ///     running and a kernel transition would be needed to run it. This sets
    ///     IORING_SQ_TASKRUN in the sq ring flags. Not valid with <see cref="CoopTaskRun" />.
    /// </summary>
    TaskRunFlag = 1 << 9,

    /// <summary>
    ///     SQEs are 128 byte
    /// </summary>
    Sqe128 = 1 << 10,

    /// <summary>
    ///     CQEs are 32 byte
    /// </summary>
    Cqe32 = 1U << 11,

    /// <summary>
    ///     Only one task is allowed to submit requests
    /// </summary>
    SingleIssuer = 1U << 12,

    /// <summary>
    ///     Defer running task work to get events.
    ///     Rather than running bits of task work whenever the task transitions
    ///     try to do it just before it is needed.
    /// </summary>
    DeferTaskRun = 1U << 13
}