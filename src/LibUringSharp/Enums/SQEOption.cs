namespace LibUringSharp.Enums;

public enum SqeOption : byte
{
    None = 0,

    /// <summary>
    ///     When this flag is specified, fd is an index into the files array registered with
    ///     the <see cref="Ring" /> instance.
    /// </summary>
    FixedFile = 1 << 1,

    /// <summary>
    ///     When this flag is specified, the SQE will not be started before previously
    ///     submitted SQEs have completed, and new SQEs will not be started before this
    ///     one completes.
    /// </summary>
    Drain = 1 << 2,

    /// <summary>
    ///     When this flag is specified, the SQE forms a link with the next SQE in the submission ring.
    /// </summary>
    Link = 1 << 3,

    /// <summary>
    ///     Like <see cref="Link" /> , except the links aren't severed if an error or unexpected result occurs.
    /// </summary>
    Hardlink = 1 << 4,

    /// <summary>
    ///     Normal operation for io_uring is to try and issue an sqe as non-blocking first,
    ///     and if that fails, execute it in an async manner. To support more efficient
    ///     overlapped operation of requests that the application knows/assumes will
    ///     always (or most of the time) block, the application can ask for an sqe to be
    ///     issued async from the start.
    /// </summary>
    Async = 1 << 5,

    /// <summary>
    ///     If set, and if the request types supports it, select an IO buffer from the indicated buffer group.
    /// </summary>
    BufferSelect = 1 << 6,

    /// <summary>
    ///     Request that no CQE be generated for this request, if it completes successfully.
    /// </summary>
    CqeSkipSuccess = 1 << 7
}