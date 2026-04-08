using System.Runtime.CompilerServices;

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
        ReadMultishot,
        WaitId,
        FutexWait,
        FutexWake,
        FutexWaitV,
        FixedFdInstall,
        FTruncate,
        Bind,
        Listen,
        RecvZc,
        EpollWait,
        ReadVFixed,
        WriteVFixed,
        Pipe,
        Nop128,
        UringCmd128,
        Last
    }

    public const uint IORING_FILE_INDEX_ALLOC = ~0U;

    public const int IOSQE_FIXED_FILE_BIT = 1;
    public const int IOSQE_IO_DRAIN_BIT = 2;
    public const int IOSQE_IO_LINK_BIT = 3;
    public const int IOSQE_IO_HARDLINK_BIT = 4;
    public const int IOSQE_ASYNC_BIT = 5;
    public const int IOSQE_BUFFER_SELECT_BIT = 6;
    public const int IOSQE_CQE_SKIP_SUCCESS_BIT = 7;

    public const byte IOSQE_FIXED_FILE = 1 << IOSQE_FIXED_FILE_BIT;
    public const byte IOSQE_IO_DRAIN = 1 << IOSQE_IO_DRAIN_BIT;
    public const byte IOSQE_IO_LINK = 1 << IOSQE_IO_LINK_BIT;
    public const byte IOSQE_IO_HARDLINK = 1 << IOSQE_IO_HARDLINK_BIT;
    public const byte IOSQE_ASYNC = 1 << IOSQE_ASYNC_BIT;
    public const byte IOSQE_BUFFER_SELECT = 1 << IOSQE_BUFFER_SELECT_BIT;
    public const byte IOSQE_CQE_SKIP_SUCCESS = 1 << IOSQE_CQE_SKIP_SUCCESS_BIT;

    public const uint IORING_SETUP_IOPOLL = 1U << 0;
    public const uint IORING_SETUP_SQPOLL = 1U << 1;
    public const uint IORING_SETUP_SQ_AFF = 1U << 2;
    public const uint IORING_SETUP_CQSIZE = 1U << 3;
    public const uint IORING_SETUP_CLAMP = 1U << 4;
    public const uint IORING_SETUP_ATTACH_WQ = 1U << 5;
    public const uint IORING_SETUP_R_DISABLED = 1U << 6;
    public const uint IORING_SETUP_SUBMIT_ALL = 1U << 7;
    public const uint IORING_SETUP_COOP_TASKRUN = 1U << 8;
    public const uint IORING_SETUP_TASKRUN_FLAG = 1U << 9;
    public const uint IORING_SETUP_SQE128 = 1U << 10;
    public const uint IORING_SETUP_CQE32 = 1U << 11;
    public const uint IORING_SETUP_SINGLE_ISSUER = 1U << 12;
    public const uint IORING_SETUP_DEFER_TASKRUN = 1U << 13;
    public const uint IORING_SETUP_NO_MMAP = 1U << 14;
    public const uint IORING_SETUP_REGISTERED_FD_ONLY = 1U << 15;
    public const uint IORING_SETUP_NO_SQARRAY = 1U << 16;

    public const uint IORING_URING_CMD_FIXED = 1U << 0;
    public const uint IORING_FSYNC_DATASYNC = 1U << 0;
    public const uint IORING_TIMEOUT_ABS = 1U << 0;
    public const uint IORING_TIMEOUT_UPDATE = 1U << 1;
    public const uint IORING_TIMEOUT_BOOTTIME = 1U << 2;
    public const uint IORING_TIMEOUT_REALTIME = 1U << 3;
    public const uint IORING_LINK_TIMEOUT_UPDATE = 1U << 4;
    public const uint IORING_TIMEOUT_ETIME_SUCCESS = 1U << 5;
    public const uint IORING_TIMEOUT_MULTISHOT = 1U << 6;
    public const uint IORING_TIMEOUT_CLOCK_MASK = IORING_TIMEOUT_BOOTTIME | IORING_TIMEOUT_REALTIME;
    public const uint IORING_TIMEOUT_UPDATE_MASK = IORING_TIMEOUT_UPDATE | IORING_LINK_TIMEOUT_UPDATE;
    public const uint SPLICE_F_FD_IN_FIXED = 1U << 31;
    public const uint IORING_POLL_ADD_MULTI = 1U << 0;
    public const uint IORING_POLL_UPDATE_EVENTS = 1U << 1;
    public const uint IORING_POLL_UPDATE_USER_DATA = 1U << 2;
    public const uint IORING_POLL_ADD_LEVEL = 1U << 3;
    public const uint IORING_ASYNC_CANCEL_ALL = 1U << 0;
    public const uint IORING_ASYNC_CANCEL_FD = 1U << 1;
    public const uint IORING_ASYNC_CANCEL_ANY = 1U << 2;
    public const uint IORING_ASYNC_CANCEL_FD_FIXED = 1U << 3;
    public const ushort IORING_RECVSEND_POLL_FIRST = 1 << 0;
    public const ushort IORING_RECV_MULTISHOT = 1 << 1;
    public const ushort IORING_RECVSEND_FIXED_BUF = 1 << 2;
    public const ushort IORING_SEND_ZC_REPORT_USAGE = 1 << 3;
    public const uint IORING_NOTIF_USAGE_ZC_COPIED = 1U << 31;
    public const uint IORING_ACCEPT_MULTISHOT = 1U << 0;
    public const int IORING_MSG_DATA = 0;
    public const int IORING_MSG_SEND_FD = 1;
    public const uint IORING_MSG_RING_CQE_SKIP = 1U << 0;
    public const uint IORING_MSG_RING_FLAGS_PASS = 1U << 1;
    public const uint IORING_FIXED_FD_NO_CLOEXEC = 1U << 0;
    public const uint IORING_CQE_F_BUFFER = 1U << 0;
    public const uint IORING_CQE_F_MORE = 1U << 1;
    public const uint IORING_CQE_F_SOCK_NONEMPTY = 1U << 2;
    public const uint IORING_CQE_F_NOTIF = 1U << 3;
    public const int IORING_CQE_BUFFER_SHIFT = 16;
    public const ulong IORING_OFF_SQ_RING = 0UL;
    public const ulong IORING_OFF_CQ_RING = 0x8000000UL;
    public const ulong IORING_OFF_SQES = 0x10000000UL;
    public const ulong IORING_OFF_PBUF_RING = 0x80000000UL;
    public const ulong IORING_OFF_PBUF_SHIFT = 16;
    public const ulong IORING_OFF_MMAP_MASK = 0xf8000000UL;
    public const uint IORING_SQ_NEED_WAKEUP = 1U << 0;
    public const uint IORING_SQ_CQ_OVERFLOW = 1U << 1;
    public const uint IORING_SQ_TASKRUN = 1U << 2;
    public const uint IORING_CQ_EVENTFD_DISABLED = 1U << 0;
    public const uint IORING_ENTER_GETEVENTS = 1U << 0;
    public const uint IORING_ENTER_SQ_WAKEUP = 1U << 1;
    public const uint IORING_ENTER_SQ_WAIT = 1U << 2;
    public const uint IORING_ENTER_EXT_ARG = 1U << 3;
    public const uint IORING_ENTER_REGISTERED_RING = 1U << 4;
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
    public const uint IORING_FEAT_REG_REG_RING = 1U << 13;
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
    public const int IORING_REGISTER_FILES2 = 13;
    public const int IORING_REGISTER_FILES_UPDATE2 = 14;
    public const int IORING_REGISTER_BUFFERS2 = 15;
    public const int IORING_REGISTER_BUFFERS_UPDATE = 16;
    public const int IORING_REGISTER_IOWQ_AFF = 17;
    public const int IORING_UNREGISTER_IOWQ_AFF = 18;
    public const int IORING_REGISTER_IOWQ_MAX_WORKERS = 19;
    public const int IORING_REGISTER_RING_FDS = 20;
    public const int IORING_UNREGISTER_RING_FDS = 21;
    public const int IORING_REGISTER_PBUF_RING = 22;
    public const int IORING_UNREGISTER_PBUF_RING = 23;
    public const int IORING_REGISTER_SYNC_CANCEL = 24;
    public const int IORING_REGISTER_FILE_ALLOC_RANGE = 25;
    public const int IORING_REGISTER_PBUF_STATUS = 26;
    public const int IORING_REGISTER_NAPI = 27;
    public const int IORING_UNREGISTER_NAPI = 28;
    public const int IORING_REGISTER_LAST = 29;
    public const int IORING_REGISTER_USE_REGISTERED_RING = 1 << 31;
    public const int IO_WQ_BOUND = 0;
    public const int IO_WQ_UNBOUND = 1;
    public const uint IORING_RSRC_REGISTER_SPARSE = 1U << 0;
    public const int IORING_REGISTER_FILES_SKIP = -2;
    public const uint IO_URING_OP_SUPPORTED = 1U << 0;
    public const int IORING_RESTRICTION_REGISTER_OP = 0;
    public const int IORING_RESTRICTION_SQE_OP = 1;
    public const int IORING_RESTRICTION_SQE_FLAGS_ALLOWED = 2;
    public const int IORING_RESTRICTION_SQE_FLAGS_REQUIRED = 3;
    public const int IORING_RESTRICTION_LAST = 4;

    private const int __NR_io_uring_setup = 425;
    private const int __NR_io_uring_enter = 426;
    private const int __NR_io_uring_register = 427;

    public static unsafe int io_uring_register(int fd, uint opcode, void* arg, uint nr_args)
    {
        var ret = (long)syscall(__NR_io_uring_register, fd, opcode, arg, nr_args);
        return ret < 0 ? -errno : (int)ret;
    }

    public static unsafe int io_uring_setup(uint entries, ref io_uring_params p)
    {
        var ret = (long)syscall(__NR_io_uring_setup, entries, Unsafe.AsPointer(ref p));
        return ret < 0 ? -errno : (int)ret;
    }

    public static int io_uring_enter(int fd, uint to_submit, uint min_complete, uint flags, ref sigset_t sig)
    {
        return io_uring_enter2(fd, to_submit, min_complete, flags, ref sig, 8);
    }

    public static unsafe int io_uring_enter2(int fd, uint to_submit, uint min_complete, uint flags, ref sigset_t sig,
        ulong size)
    {
        var ret = (long)syscall(__NR_io_uring_enter, fd, to_submit, min_complete, flags, Unsafe.AsPointer(ref sig),
            size);
        return ret < 0 ? -errno : (int)ret;
    }
}
