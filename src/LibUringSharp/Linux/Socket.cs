using System.Runtime.InteropServices;

namespace LibUringSharp.Linux;

public static partial class LibC
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct MsgHeader
    {
        /// <summary>
        ///     Address to send to/receive from.
        /// </summary>
        private readonly void* msg_name;

        /// <summary>
        ///     Length of address data.
        /// </summary>
        private readonly uint msg_namelen;

        /// <summary>
        ///     Vector of data to send/receive into.
        /// </summary>
        private readonly IoVector* msg_iov;

        /// <summary>
        ///     Number of elements in the vector.
        /// </summary>
        private readonly ulong msg_iovlen;

        /// <summary>
        ///     Ancillary data (eg BSD filedesc passing).
        /// </summary>
        private readonly void* msg_control;

        /// <summary>
        ///     Ancillary data buffer length.
        ///     The type should be socklen_t but the definition of the kernel is incompatible with this.
        /// </summary>
        private readonly ulong msg_control_len;

        /// <summary>
        ///     Flags on received message.
        /// </summary>
        private readonly int msg_flags;
    }
}