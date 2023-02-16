using System.Runtime.InteropServices;

namespace QRWells.LibUringSharp.Linux;

public static partial class LibC
{
    [StructLayout(LayoutKind.Sequential)]
    public struct statx_timestamp
    {
        public long tv_sec;
        public uint tv_nsec;
        public int __reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct StatX
    {
        /// <summary>
        ///     What results were written
        /// </summary>
        public readonly uint stx_mask;

        /// <summary>
        ///     Preferred general I/O size
        /// </summary>
        public readonly uint stx_blksize;

        /// <summary>
        ///     Flags conveying information about the file
        /// </summary>
        public readonly ulong stx_attributes;

        /// <summary>
        ///     Number of hard links
        /// </summary>
        public readonly uint stx_nlink;

        /// <summary>
        ///     User ID of owner
        /// </summary>
        public readonly uint stx_uid;

        /// <summary>
        ///     Group ID of owner
        /// </summary>
        public readonly uint stx_gid;

        /// <summary>
        ///     File mode
        /// </summary>
        public readonly ushort stx_mode;

        public readonly ushort __spare0;

        /// <summary>
        ///     Inode number
        /// </summary>
        public readonly ulong stx_ino;

        /// <summary>
        ///     File size
        /// </summary>
        public readonly ulong stx_size;

        /// <summary>
        ///     Number of 512-byte blocks allocated
        /// </summary>
        public readonly ulong stx_blocks;

        /// <summary>
        ///     Mask to show what's supported in stx_attributes
        /// </summary>
        public readonly ulong stx_attributes_mask;

        /// <summary>
        ///     Last access time
        /// </summary>
        public readonly statx_timestamp stx_atime;

        /// <summary>
        ///     File creation time
        /// </summary>
        public readonly statx_timestamp stx_btime;

        /// <summary>
        ///     Last attribute change time
        /// </summary>
        public readonly statx_timestamp stx_ctime;

        /// <summary>
        ///     Last data modification time
        /// </summary>
        public readonly statx_timestamp stx_mtime;

        /// <summary>
        ///     Device ID of special file [if bdev/cdev]
        /// </summary>
        public readonly uint stx_rdev_major;

        public readonly uint stx_rdev_minor;

        /// <summary>
        ///     ID of device containing file
        /// </summary>
        public readonly uint stx_dev_major;

        public readonly uint stx_dev_minor;

        public readonly ulong stx_mnt_id;

        public readonly ulong __spare2;

        /// <summary>
        ///     Spare space for future expansion
        /// </summary>
        public unsafe fixed ulong __spare3[12];
    }
}