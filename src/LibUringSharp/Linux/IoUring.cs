using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QRWells.LibUringSharp.Linux;

public static partial class LibC
{
    public enum IoUringOp : byte
    {
        Nop,
        ReadV,
        WriteV,
        FSync,
        ReadFixed,
        WriteFixed,
        PollAdd,
        PollRemove,
        SyncFileRange,
        SendMsg,
        ReceiveMsg,
        Timeout,
        TimeoutRemove,
        Accept,
        AsyncCancel,
        LinkTimeout,
        Connect,
        FAllocate,
        OpenAt,
        Close,
        FilesUpdate,
        StatX,
        Read,
        Write,
        FAdvise,
        MAdvise,
        Send,
        Receive,
        OpenAt2,
        EpollCtl,
        Splice,
        ProvideBuffers,
        RemoveBuffers,
        Tee,
        Shutdown,
        RenameAt,
        UnlinkAt,
        MkdirAt,
        SymlinkAt,
        LinkAt,
        MsgRing,
        FSetXAttr,
        SetXAttr,
        FGetXAttr,
        GetXAttr,
        Socket,
        UringCmd,
        SendZc,
        SendMsgZc,

        /* this goes last, obviously */
        Last
    }

    /// <summary>
    ///     If sqe->file_index is set to this for opcodes that instantiate a new
    ///     direct descriptor (like openat/openat2/accept), then io_uring will allocate
    ///     an available direct descriptor instead of having the application pass one
    ///     in. The picked direct descriptor will be returned in cqe->res, or -ENFILE
    ///     if the space is full.
    /// </summary>
    public const uint IORING_FILE_INDEX_ALLOC = ~0U;

    public const int IOSQE_FIXED_FILE_BIT = 1;
    public const int IOSQE_IO_DRAIN_BIT = 2;
    public const int IOSQE_IO_LINK_BIT = 3;
    public const int IOSQE_IO_HARDLINK_BIT = 4;
    public const int IOSQE_ASYNC_BIT = 5;
    public const int IOSQE_BUFFER_SELECT_BIT = 6;
    public const int IOSQE_CQE_SKIP_SUCCESS_BIT = 7;

    /*
     * sqe->flags
     */
    /* use fixed fileset */
    public const byte IOSQE_FIXED_FILE = 1 << IOSQE_FIXED_FILE_BIT;

    /* issue after inflight IO */
    public const byte IOSQE_IO_DRAIN = 1 << IOSQE_IO_DRAIN_BIT;

    /* links next sqe */
    public const byte IOSQE_IO_LINK = 1 << IOSQE_IO_LINK_BIT;

    /* like LINK, but stronger */
    public const byte IOSQE_IO_HARDLINK = 1 << IOSQE_IO_HARDLINK_BIT;

    /* always go async */
    public const byte IOSQE_ASYNC = 1 << IOSQE_ASYNC_BIT;

    /* select buffer from sqe->buf_group */
    public const byte IOSQE_BUFFER_SELECT = 1 << IOSQE_BUFFER_SELECT_BIT;

    /* don't post CQE if request succeeded */
    public const byte IOSQE_CQE_SKIP_SUCCESS = 1 << IOSQE_CQE_SKIP_SUCCESS_BIT;

    /*
     * io_uring_setup() flags
     */
    public const uint IORING_SETUP_IOPOLL = 1U << 0; /* io_context is polled */
    public const uint IORING_SETUP_SQPOLL = 1U << 1; /* SQ poll thread */
    public const uint IORING_SETUP_SQ_AFF = 1U << 2; /* sq_thread_cpu is valid */
    public const uint IORING_SETUP_CQSIZE = 1U << 3; /* app defines CQ size */
    public const uint IORING_SETUP_CLAMP = 1U << 4; /* clamp SQ/CQ ring sizes */
    public const uint IORING_SETUP_ATTACH_WQ = 1U << 5; /* attach to existing wq */
    public const uint IORING_SETUP_R_DISABLED = 1U << 6; /* start with ring disabled */
    public const uint IORING_SETUP_SUBMIT_ALL = 1U << 7; /* continue submit on error */

    /*
     * Cooperative task running. When requests complete, they often require
     * forcing the submitter to transition to the kernel to complete. If this
     * flag is set, work will be done when the task transitions anyway, rather
     * than force an inter-processor interrupt reschedule. This avoids interrupting
     * a task running in userspace, and saves an IPI.
     */
    public const uint IORING_SETUP_COOP_TASKRUN = 1U << 8;

    /*
     * If COOP_TASKRUN is set, get notified if task work is available for
     * running and a kernel transition would be needed to run it. This sets
     * IORING_SQ_TASKRUN in the sq ring flags. Not valid with COOP_TASKRUN.
     */
    public const uint IORING_SETUP_TASKRUN_FLAG = 1U << 9;
    public const uint IORING_SETUP_SQE128 = 1U << 10; /* SQEs are 128 byte */

    public const uint IORING_SETUP_CQE32 = 1U << 11; /* CQEs are 32 byte */

    /*
     * Only one task is allowed to submit requests
     */
    public const uint IORING_SETUP_SINGLE_ISSUER = 1U << 12;

    /*
     * Defer running task work to get events.
     * Rather than running bits of task work whenever the task transitions
     * try to do it just before it is needed.
     */
    public const uint IORING_SETUP_DEFER_TASKRUN = 1U << 13;

    /*
     * sqe->uring_cmd_flags
     * IORING_URING_CMD_FIXED	use registered buffer; pass this flag
     *				along with setting sqe->buf_index.
     */
    public const uint IORING_URING_CMD_FIXED = 1U << 0;


    /*
     * sqe->fsync_flags
     */
    public const uint IORING_FSYNC_DATASYNC = 1U << 0;

    /*
     * sqe->timeout_flags
     */
    public const uint IORING_TIMEOUT_ABS = 1U << 0;
    public const uint IORING_TIMEOUT_UPDATE = 1U << 1;
    public const uint IORING_TIMEOUT_BOOTTIME = 1U << 2;
    public const uint IORING_TIMEOUT_REALTIME = 1U << 3;
    public const uint IORING_LINK_TIMEOUT_UPDATE = 1U << 4;
    public const uint IORING_TIMEOUT_ETIME_SUCCESS = 1U << 5;
    public const uint IORING_TIMEOUT_CLOCK_MASK = IORING_TIMEOUT_BOOTTIME | IORING_TIMEOUT_REALTIME;

    public const uint IORING_TIMEOUT_UPDATE_MASK = IORING_TIMEOUT_UPDATE | IORING_LINK_TIMEOUT_UPDATE;

    /*
     * sqe->splice_flags
     * extends splice(2) flags
     */
    public const uint SPLICE_F_FD_IN_FIXED = 1U << 31; /* the last bit of uint */

    /*
     * POLL_ADD flags. Note that since sqe->poll_events is the flag space, the
     * command flags for POLL_ADD are stored in sqe->len.
     *
     * IORING_POLL_ADD_MULTI	Multishot poll. Sets IORING_CQE_F_MORE if
     *				the poll handler will continue to report
     *				CQEs on behalf of the same SQE.
     *
     * IORING_POLL_UPDATE		Update existing poll request, matching
     *				sqe->addr as the old user_data field.
     *
     * IORING_POLL_LEVEL		Level triggered poll.
     */
    public const uint IORING_POLL_ADD_MULTI = 1U << 0;
    public const uint IORING_POLL_UPDATE_EVENTS = 1U << 1;
    public const uint IORING_POLL_UPDATE_USER_DATA = 1U << 2;
    public const uint IORING_POLL_ADD_LEVEL = 1U << 3;

    /*
     * ASYNC_CANCEL flags.
     *
     * IORING_ASYNC_CANCEL_ALL	Cancel all requests that match the given key
     * IORING_ASYNC_CANCEL_FD	Key off 'fd' for cancelation rather than the
     *				request 'user_data'
     * IORING_ASYNC_CANCEL_ANY	Match any request
     * IORING_ASYNC_CANCEL_FD_FIXED	'fd' passed in is a fixed descriptor
     */
    public const uint IORING_ASYNC_CANCEL_ALL = 1U << 0;
    public const uint IORING_ASYNC_CANCEL_FD = 1U << 1;
    public const uint IORING_ASYNC_CANCEL_ANY = 1U << 2;
    public const uint IORING_ASYNC_CANCEL_FD_FIXED = 1U << 3;

    /*
     * send/sendmsg and recv/recvmsg flags (sqe->ioprio)
     *
     * IORING_RECVSEND_POLL_FIRST	If set, instead of first attempting to send
     *				or receive and arm poll if that yields an
     *				-EAGAIN result, arm poll upfront and skip
     *				the initial transfer attempt.
     *
     * IORING_RECV_MULTISHOT	Multishot recv. Sets IORING_CQE_F_MORE if
     *				the handler will continue to report
     *				CQEs on behalf of the same SQE.
     *
     * IORING_RECVSEND_FIXED_BUF	Use registered buffers, the index is stored in
     *				the buf_index field.
     *
     * IORING_SEND_ZC_REPORT_USAGE
     *				If set, SEND[MSG]_ZC should report
     *				the zerocopy usage in cqe.res
     *				for the IORING_CQE_F_NOTIF cqe.
     *				0 is reported if zerocopy was actually possible.
     *				IORING_NOTIF_USAGE_ZC_COPIED if data was copied
     *				(at least partially).
     */
    public const ushort IORING_RECVSEND_POLL_FIRST = 1 << 0;
    public const ushort IORING_RECV_MULTISHOT = 1 << 1;
    public const ushort IORING_RECVSEND_FIXED_BUF = 1 << 2;
    public const ushort IORING_SEND_ZC_REPORT_USAGE = 1 << 3;

    /*
     * cqe.res for IORING_CQE_F_NOTIF if
     * IORING_SEND_ZC_REPORT_USAGE was requested
     *
     * It should be treated as a flag, all other
     * bits of cqe.res should be treated as reserved!
     */
    public const uint IORING_NOTIF_USAGE_ZC_COPIED = 1U << 31;

    /*
     * accept flags stored in sqe->ioprio
     */
    public const uint IORING_ACCEPT_MULTISHOT = 1U << 0;

    public const int IORING_MSG_DATA = 0; /* pass sqe->len as 'res' and off as user_data */
    public const int IORING_MSG_SEND_FD = 1; /* send a registered fd to another ring */

    /*
     * IORING_OP_MSG_RING flags (sqe->msg_ring_flags)
     *
     * IORING_MSG_RING_CQE_SKIP	Don't post a CQE to the target ring. Not
     *				applicable for IORING_MSG_DATA, obviously.
     */
    public const uint IORING_MSG_RING_CQE_SKIP = 1U << 0;

    /* Pass through the flags from sqe->file_index to cqe->flags */
    public const uint IORING_MSG_RING_FLAGS_PASS = 1U << 1;

    /*
     * cqe->flags
     *
     * IORING_CQE_F_BUFFER	If set, the upper 16 bits are the buffer ID
     * IORING_CQE_F_MORE	If set, parent SQE will generate more CQE entries
     * IORING_CQE_F_SOCK_NONEMPTY	If set, more data to read after socket recv
     * IORING_CQE_F_NOTIF	Set for notification CQEs. Can be used to distinct
     * 			them from sends.
     */
    public const uint IORING_CQE_F_BUFFER = 1U << 0;
    public const uint IORING_CQE_F_MORE = 1U << 1;
    public const uint IORING_CQE_F_SOCK_NONEMPTY = 1U << 2;
    public const uint IORING_CQE_F_NOTIF = 1U << 3;


    public const int IORING_CQE_BUFFER_SHIFT = 16;

    /*
     * Magic offsets for the application to mmap the data it needs
     */
    public const ulong IORING_OFF_SQ_RING = 0UL;
    public const ulong IORING_OFF_CQ_RING = 0x8000000UL;
    public const ulong IORING_OFF_SQES = 0x10000000UL;

    /*
     * sq_ring->flags
     */
    public const uint IORING_SQ_NEED_WAKEUP = 1U << 0; /* needs io_uring_enter wakeup */
    public const uint IORING_SQ_CQ_OVERFLOW = 1U << 1; /* CQ ring is overflown */
    public const uint IORING_SQ_TASKRUN = 1U << 2; /* task should enter the kernel */

    /*
     * cq_ring->flags
     */

    /* disable eventfd notifications */
    public const uint IORING_CQ_EVENTFD_DISABLED = 1U << 0;

    /*
     * io_uring_enter(2) flags
     */
    public const uint IORING_ENTER_GETEVENTS = 1U << 0;
    public const uint IORING_ENTER_SQ_WAKEUP = 1U << 1;
    public const uint IORING_ENTER_SQ_WAIT = 1U << 2;
    public const uint IORING_ENTER_EXT_ARG = 1U << 3;
    public const uint IORING_ENTER_REGISTERED_RING = 1U << 4;

    /*
     * io_uring_params->features flags
     */
    public const uint IORING_FEAT_SINGLE_MMAP = 1U << 0;
    public const uint IORING_FEAT_NODROP = 1U << 1;
    public const uint IORING_FEAT_SUBMIT_STABLE = 1U << 2;
    public const uint IORING_FEAT_RW_CUR_POS = 1U << 3;
    public const uint IORING_FEAT_CUR_PERSONALITY = 1U << 4;
    public const uint IORING_FEAT_FAST_POLL = 1U << 5;
    public const uint IORING_FEAT_POLL_32BITS = 1U << 6;
    public const uint IORING_FEAT_SQPOLL_NONFIXED = 1U << 7;
    public const uint IORING_FEAT_EXT_ARG = 1U << 8;
    public const uint IORING_FEAT_NATIVE_WORKERS = 1U << 9;
    public const uint IORING_FEAT_RSRC_TAGS = 1U << 10;
    public const uint IORING_FEAT_CQE_SKIP = 1U << 11;
    public const uint IORING_FEAT_LINKED_FILE = 1U << 12;

    /*
     * io_uring_register(2) opcodes and arguments
     */
    public const int IORING_REGISTER_BUFFERS = 0;
    public const int IORING_UNREGISTER_BUFFERS = 1;
    public const int IORING_REGISTER_FILES = 2;
    public const int IORING_UNREGISTER_FILES = 3;
    public const int IORING_REGISTER_EVENTFD = 4;
    public const int IORING_UNREGISTER_EVENTFD = 5;
    public const int IORING_REGISTER_FILES_UPDATE = 6;
    public const int IORING_REGISTER_EVENTFD_ASYNC = 7;
    public const int IORING_REGISTER_PROBE = 8;
    public const int IORING_REGISTER_PERSONALITY = 9;
    public const int IORING_UNREGISTER_PERSONALITY = 10;
    public const int IORING_REGISTER_RESTRICTIONS = 11;
    public const int IORING_REGISTER_ENABLE_RINGS = 12;

    /* extended with tagging */
    public const int IORING_REGISTER_FILES2 = 13;
    public const int IORING_REGISTER_FILES_UPDATE2 = 14;
    public const int IORING_REGISTER_BUFFERS2 = 15;
    public const int IORING_REGISTER_BUFFERS_UPDATE = 16;

    /* set/clear io-wq thread affinities */
    public const int IORING_REGISTER_IOWQ_AFF = 17;
    public const int IORING_UNREGISTER_IOWQ_AFF = 18;

    /* set/get max number of io-wq workers */
    public const int IORING_REGISTER_IOWQ_MAX_WORKERS = 19;

    /* register/unregister io_uring fd with the ring */
    public const int IORING_REGISTER_RING_FDS = 20;
    public const int IORING_UNREGISTER_RING_FDS = 21;

    /* register ring based provide buffer group */
    public const int IORING_REGISTER_PBUF_RING = 22;
    public const int IORING_UNREGISTER_PBUF_RING = 23;

    /* sync cancelation API */
    public const int IORING_REGISTER_SYNC_CANCEL = 24;

    /* register a range of fixed file slots for automatic slot allocation */
    public const int IORING_REGISTER_FILE_ALLOC_RANGE = 25;
    public const int IORING_REGISTER_LAST = 26;

    /* io-wq worker categories */

    public const int IO_WQ_BOUND = 0;

    public const int IO_WQ_UNBOUND = 1;

    /// <summary>
    ///     Register a fully sparse file space, rather than pass in an array of all -1 file descriptors.
    /// </summary>
    public const uint IORING_RSRC_REGISTER_SPARSE = 1U << 0;

    /// <summary>
    ///     Skip updating fd indexes set to this value in the fd table
    /// </summary>
    public const int IORING_REGISTER_FILES_SKIP = -2;

    public const uint IO_URING_OP_SUPPORTED = 1U << 0;

    /*
     * io_uring_restriction->opcode values
     */

    /// <summary>
    ///     Allow an io_uring_register(2) opcode
    /// </summary>
    public const int IORING_RESTRICTION_REGISTER_OP = 0;

    /// <summary>
    ///     Allow an sqe opcode
    /// </summary>
    public const int IORING_RESTRICTION_SQE_OP = 1;

    /// <summary>
    ///     Allow sqe flags
    /// </summary>
    public const int IORING_RESTRICTION_SQE_FLAGS_ALLOWED = 2;

    /// <summary>
    ///     Require sqe flags (these flags must be set on each submission)
    /// </summary>
    public const int IORING_RESTRICTION_SQE_FLAGS_REQUIRED = 3;

    public const int IORING_RESTRICTION_LAST = 4;

    private const int __NR_io_uring_setup = 425;
    private const int __NR_io_uring_enter = 426;
    private const int __NR_io_uring_register = 427;

    public static unsafe int io_uring_register(int fd, uint opcode, void* arg, uint nr_args)
    {
        return (int)syscall(__NR_io_uring_register, fd, opcode, arg, nr_args);
    }

    public static unsafe int io_uring_setup(uint entries, ref io_uring_params p)
    {
        return (int)syscall(__NR_io_uring_setup, entries, Unsafe.AsPointer(ref p));
    }

    public static int io_uring_enter(int fd, uint to_submit, uint min_complete, uint flags, ref sigset_t sig)
    {
        return io_uring_enter2(fd, to_submit, min_complete, flags, ref sig, 8);
    }

    public static unsafe int io_uring_enter2(int fd, uint to_submit, uint min_complete, uint flags, ref sigset_t sig,
        ulong size)
    {
        return (int)syscall(__NR_io_uring_enter, fd, to_submit, min_complete, flags, Unsafe.AsPointer(ref sig), size);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_params
    {
        public uint sq_entries;
        public uint cq_entries;
        public uint flags;
        public uint sq_thread_cpu;
        public uint sq_thread_idle;
        public uint features;
        public uint wq_fd;
        public unsafe fixed uint resv[3];
        public io_sqring_offsets sq_off;
        public io_cqring_offsets cq_off;
    }

    /// <summary>
    ///     IO submission data structure (Submission Queue Entry)
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    public struct io_uring_sqe
    {
        [FieldOffset(0)] public byte opcode; /* type of operation for this sqe */
        [FieldOffset(1)] public byte flags; /* IOSQE_ flags */
        [FieldOffset(2)] public ushort ioprio; /* ioprio for the request */
        [FieldOffset(4)] public int fd; /* file descriptor to do IO on */

        [FieldOffset(8)] public ulong off; /* offset into file */
        [FieldOffset(8)] public ulong addr2;
        [FieldOffset(8)] public uint cmd_op;
        [FieldOffset(12)] public uint __pad1;


        [FieldOffset(16)] public ulong addr; /* pointer to buffer or iovecs */
        [FieldOffset(16)] public ulong splice_off_in;

        [FieldOffset(24)] public uint len; /* buffer size or number of iovecs */


        [FieldOffset(28)] public int rw_flags;
        [FieldOffset(28)] public uint fsync_flags;
        [FieldOffset(28)] public ushort poll_events; /* compatibility */
        [FieldOffset(28)] public uint poll32_events; /* word-reversed for BE */
        [FieldOffset(28)] public uint sync_range_flags;
        [FieldOffset(28)] public uint msg_flags;
        [FieldOffset(28)] public uint timeout_flags;
        [FieldOffset(28)] public uint accept_flags;
        [FieldOffset(28)] public uint cancel_flags;
        [FieldOffset(28)] public uint open_flags;
        [FieldOffset(28)] public uint statx_flags;
        [FieldOffset(28)] public uint fadvise_advice;
        [FieldOffset(28)] public uint splice_flags;
        [FieldOffset(28)] public uint rename_flags;
        [FieldOffset(28)] public uint unlink_flags;
        [FieldOffset(28)] public uint hardlink_flags;
        [FieldOffset(28)] public uint xattr_flags;
        [FieldOffset(28)] public uint msg_ring_flags;
        [FieldOffset(28)] public uint uring_cmd_flags;

        [FieldOffset(32)] public ulong user_data; /* data to be passed back at completion time */

        /* pack this to avoid bogus arm OABI complaints */
        /* index into fixed buffers, if used */
        [FieldOffset(40)] public ushort buf_index;

        /* for grouped buffer selection */
        [FieldOffset(40)] public ushort buf_group;

        /* personality to use, if used */
        [FieldOffset(42)] public ushort personality;

        [FieldOffset(44)] public int splice_fd_in;
        [FieldOffset(44)] public uint file_index;
        [FieldOffset(44)] public ushort addr_len;
        [FieldOffset(46)] public ushort __pad3;


        [FieldOffset(48)] public ulong addr3;
        [FieldOffset(56)] public ulong __pad2;

        /// <summary>
        ///     If the ring is initialized with IORING_SETUP_SQE128,
        ///     then this field is used for 80 bytes of arbitrary command data
        /// </summary>
        public static unsafe byte* cmd(io_uring_sqe* probe)
        {
            return (byte*)probe + 1;
        }

        public const ulong Size = 64;
    }

    /// <summary>
    ///     IO completion data structure (Completion Queue Entry)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_cqe
    {
        /// <summary>
        ///     sqe->data submission passed back
        /// </summary>
        public ulong user_data;

        /// <summary>
        ///     result code for this event
        /// </summary>
        public int res;

        public uint flags;

        /// <summary>
        ///     If the ring is initialized with IORING_SETUP_CQE32, then this field
        ///     contains 16-bytes of padding, doubling the size of the CQE.
        /// </summary>
        /// <param name="cqe"></param>
        /// <returns></returns>
        public static unsafe ulong* big_cqe(io_uring_cqe* cqe)
        {
            return (ulong*)(cqe + 1);
        }

        public const int Size = 16;
    }

    /// <summary>
    ///     Filled with the offset for mmap(2)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct io_sqring_offsets
    {
        public uint head;
        public uint tail;
        public uint ring_mask;
        public uint ring_entries;
        public uint flags;
        public uint dropped;
        public uint array;
        public uint resv1;
        public ulong resv2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_cqring_offsets
    {
        public uint head;
        public uint tail;
        public uint ring_mask;
        public uint ring_entries;
        public uint overflow;
        public uint cqes;
        public uint flags;
        public uint resv1;
        public ulong resv2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_rsrc_register
    {
        public uint nr;
        public uint flags;
        public ulong resv2;
        public ulong data;
        public ulong tags;

        public const uint Size = 32;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_rsrc_update
    {
        public uint offset;
        public uint resv;
        public ulong data;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_rsrc_update2
    {
        public uint offset;
        public uint resv;
        public ulong data;
        public ulong tags;
        public uint nr;
        public uint resv2;

        public const uint Size = 32;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_notification_slot
    {
        private readonly ulong tag;
        private unsafe fixed ulong resv[3];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_notification_register
    {
        private readonly uint nr_slots;
        private readonly uint resv;
        private readonly ulong resv2;
        private readonly ulong data;
        private readonly ulong resv3;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_probe_op
    {
        public byte op;
        public byte resv;

        /// <summary>
        ///     IO_URING_OP_* flags
        /// </summary>
        public ushort flags;

        public uint resv2;
        public const uint Size = 8;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_probe
    {
        /// <summary>
        ///     last opcode supported
        /// </summary>
        public byte last_op;

        /// <summary>
        ///     length of ops[] array below
        /// </summary>
        public byte ops_len;

        public ushort resv;
        public unsafe fixed uint resv2[3];

        public static unsafe io_uring_probe_op* ops(io_uring_probe* p)
        {
            return (io_uring_probe_op*)(p + 1);
        }

        public const uint Size = 16;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct io_uring_restriction
    {
        [FieldOffset(0)] public readonly ushort opcode;

        /// <summary>
        ///     IORING_RESTRICTION_REGISTER_OP
        /// </summary>
        [FieldOffset(2)] public readonly byte register_op;

        /// <summary>
        ///     IORING_RESTRICTION_SQE_OP
        /// </summary>
        [FieldOffset(2)] public readonly byte sqe_op;

        /// <summary>
        ///     IORING_RESTRICTION_SQE_FLAGS_*
        /// </summary>
        [FieldOffset(2)] public readonly byte sqe_flags;

        [FieldOffset(3)] public readonly byte resv;
        [FieldOffset(4)] public unsafe fixed uint resv2[3];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_buf
    {
        public ulong addr;
        public uint len;
        public ushort bid;
        public ushort resv;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_buf_ring
    {
        public readonly ulong resv1;
        public readonly uint resv2;
        public readonly ushort resv3;
        public ushort tail;

        public static unsafe io_uring_buf* bufs(io_uring_buf_ring* r)
        {
            return (io_uring_buf*)r;
        }
    }

    /// <summary>
    ///     argument for IORING_(UN)REGISTER_PBUF_RING
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_buf_reg
    {
        public ulong ring_addr;
        public uint ring_entries;
        public ushort bgid;
        public ushort pad;
        public unsafe fixed ulong resv[3];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_getevents_arg
    {
        private readonly ulong sigmask;
        private readonly uint sigmask_sz;
        private readonly uint pad;
        private readonly ulong ts;
    }

    /// <summary>
    ///     Argument for IORING_REGISTER_SYNC_CANCEL
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_sync_cancel_reg
    {
        private readonly ulong addr;
        private readonly int fd;
        private readonly uint flags;

        private readonly __kernel_timespec timeout;

        private unsafe fixed ulong pad[4];
    }

    /// <summary>
    ///     Argument for IORING_REGISTER_FILE_ALLOC_RANGE
    ///     The range is specified as [off, off + len)
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_file_index_range
    {
        public uint off;
        public uint len;
        public ulong resv;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_recvmsg_out
    {
        private readonly uint namelen;
        private readonly uint controllen;
        private readonly uint payloadlen;
        private readonly uint flags;
    }
}