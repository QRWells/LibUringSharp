namespace LibUringSharp.Enums;

[Flags]
public enum RingSetup : uint
{
    None = 0,

    /// <summary>
    ///     Perform busy-waiting for an I/O completion,
    ///     as opposed to getting notifications via an asynchronous IRQ.
    /// </summary>
    KernelIoPolling = 1 << 0,

    /// <summary>
    ///     When this flag is specified, a kernel thread is created to perform submission queue polling.
    /// </summary>
    KernelSubmissionQueuePolling = 1 << 1,

    /// <summary>
    ///     If this flag is specified, then the poll thread will be bound to the cpu set in the
    ///     <see cref="Linux.LibC.io_uring_params.sq_thread_cpu" />
    ///     field of the struct <see cref="Linux.LibC.io_uring_params" />.
    ///     This flag is only meaningful when <see cref="KernelSubmissionQueuePolling" /> is specified.
    /// </summary>
    SqPollingThreadCpuAffinity = 1 << 2,

    /// <summary>
    ///     Create the completion queue with <see cref="Linux.LibC.io_uring_params.cq_entries" /> entries
    ///     in <see cref="Linux.LibC.io_uring_params" />.
    ///     The value must be greater than entries, and may be rounded up to the next power-of-two.
    /// </summary>
    CompletionQueueSize = 1 << 3,

    /// <summary>
    ///     If this flag is specified, and if entries exceeds IORING_MAX_ENTRIES,
    ///     then entries will be clamped at IORING_MAX_ENTRIES . If the flag IORING_SETUP_SQPOLL is set,
    ///     and if the value of struct io_uring_params.cq_entries exceeds IORING_MAX_CQ_ENTRIES,
    ///     then it will be clamped at IORING_MAX_CQ_ENTRIES .
    /// </summary>
    ClampQueueRingSize = 1 << 4,

    /// <summary>
    ///     When set, the io_uring instance being created will share
    ///     the asynchronous worker thread backend of the specified io_uring ring,
    ///     rather than create a new separate thread pool.
    /// </summary>
    AttachWorkerQueue = 1 << 5,

    /// <summary>
    ///     If this flag is specified, the <see cref="Ring" /> starts in a disabled state.
    /// </summary>
    RingDisabled = 1 << 6,

    /// <summary>
    ///     If the ring is created with this flag,
    ///     <see cref="Ring" /> will continue submitting requests
    ///     even if it encounters an error submitting a request.
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
    ///     If set, <see cref="Ring" /> will use 128-byte SQEs rather than the normal 64-byte sized variant.
    /// </summary>
    Sqe128 = 1 << 10,

    /// <summary>
    ///     If set, <see cref="Ring" /> will use 32-byte CQEs rather than the normal 16-byte sized variant.
    /// </summary>
    Cqe32 = 1U << 11,

    /// <summary>
    ///     A hint to the kernel that only a single task (or thread) will submit requests,
    ///     which is used for internal optimisations.
    /// </summary>
    SingleIssuer = 1U << 12,

    /// <summary>
    ///     Defer running task work to get events.
    ///     Rather than running bits of task work whenever the task transitions
    ///     try to do it just before it is needed.
    /// </summary>
    DeferTaskRun = 1U << 13
}