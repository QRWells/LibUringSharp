using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QRWells.LibUringSharp.Linux;

public static partial class LibC
{
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

    [StructLayout(LayoutKind.Explicit)]
    public struct io_uring_sqe
    {
        [FieldOffset(0)] public byte opcode;
        [FieldOffset(1)] public byte flags;
        [FieldOffset(2)] public ushort ioprio;
        [FieldOffset(4)] public int fd;
        [FieldOffset(8)] public ulong off;
        [FieldOffset(8)] public ulong addr2;
        [FieldOffset(8)] public uint cmd_op;
        [FieldOffset(12)] public uint __pad1;
        [FieldOffset(16)] public ulong addr;
        [FieldOffset(16)] public ulong splice_off_in;
        [FieldOffset(16)] public uint level;
        [FieldOffset(20)] public uint optname;
        [FieldOffset(24)] public uint len;
        [FieldOffset(28)] public int rw_flags;
        [FieldOffset(28)] public uint fsync_flags;
        [FieldOffset(28)] public ushort poll_events;
        [FieldOffset(28)] public uint poll32_events;
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
        [FieldOffset(28)] public uint waited_flags;
        [FieldOffset(28)] public uint futex_flags;
        [FieldOffset(28)] public uint install_fd_flags;
        [FieldOffset(28)] public uint pipe_flags;
        [FieldOffset(32)] public ulong user_data;
        [FieldOffset(40)] public ushort buf_index;
        [FieldOffset(40)] public ushort buf_group;
        [FieldOffset(42)] public ushort personality;
        [FieldOffset(44)] public int splice_fd_in;
        [FieldOffset(44)] public uint file_index;
        [FieldOffset(44)] public uint zcrx_ifq_idx;
        [FieldOffset(44)] public uint optlen;
        [FieldOffset(44)] public ushort addr_len;
        [FieldOffset(46)] public ushort __pad3;
        [FieldOffset(48)] public ulong addr3;
        [FieldOffset(56)] public ulong __pad2;
        [FieldOffset(48)] public ulong attr_ptr;
        [FieldOffset(56)] public ulong attr_type_mask;
        [FieldOffset(48)] public ulong optval;

        public const int CmdOffset = 48;
        public const int CmdSize = 16;
        public const int CmdSize128 = 80;

        /// <summary>
        /// Gets a pointer to the start of the inline command payload.
        /// The payload is 16 bytes in a 64-byte SQE and 80 bytes when
        /// the ring is initialized with IORING_SETUP_SQE128.
        /// </summary>
        public static unsafe byte* cmd(io_uring_sqe* sqe)
        {
            return (byte*)sqe + CmdOffset;
        }

        public static int cmd_size(bool sqe128)
        {
            return sqe128 ? CmdSize128 : CmdSize;
        }

        public static unsafe Span<byte> cmd_span(io_uring_sqe* sqe, bool sqe128)
        {
            return new Span<byte>(cmd(sqe), cmd_size(sqe128));
        }

        public static unsafe ref T cmd_as<T>(io_uring_sqe* sqe, bool sqe128) where T : unmanaged
        {
            var span = cmd_span(sqe, sqe128);
            if (Unsafe.SizeOf<T>() > span.Length)
                throw new ArgumentOutOfRangeException(nameof(T),
                    $"Type {typeof(T).Name} exceeds available cmd payload of {span.Length} bytes.");

            return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
        }

        public const ulong Size = 64;
        public const ulong Size128 = 128;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_cqe
    {
        public ulong user_data;
        public int res;
        public uint flags;

        public static unsafe ulong* big_cqe(io_uring_cqe* cqe)
        {
            return (ulong*)(cqe + 1);
        }

        public const int Size = 16;
    }

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
        public ulong user_addr;
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
        public ulong user_addr;
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
        public ushort flags;
        public uint resv2;
        public const uint Size = 8;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_probe
    {
        public byte last_op;
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
        [FieldOffset(2)] public readonly byte register_op;
        [FieldOffset(2)] public readonly byte sqe_op;
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

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_buf_reg
    {
        public ulong ring_addr;
        public uint ring_entries;
        public ushort bgid;
        public ushort flags;
        public unsafe fixed ulong resv[3];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_buf_status
    {
        public uint buf_group;
        public uint head;
        public unsafe fixed uint resv[8];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_napi
    {
        public uint busy_poll_to;
        public byte prefer_busy_poll;
        public unsafe fixed byte pad[3];
        public ulong resv;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_getevents_arg
    {
        private readonly ulong sigmask;
        private readonly uint sigmask_sz;
        private readonly uint pad;
        private readonly ulong ts;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_sync_cancel_reg
    {
        private readonly ulong addr;
        private readonly int fd;
        private readonly uint flags;
        private readonly __kernel_timespec timeout;
        private unsafe fixed ulong pad[4];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct io_uring_file_index_range
    {
        public uint off;
        public uint len;
        public ulong resv;
    }
}
