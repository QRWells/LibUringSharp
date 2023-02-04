using System.Runtime.InteropServices;

namespace Linux;

public static partial class LibC
{
    public struct statx_timestamp
    {
        long tv_sec;
        uint tv_nsec;
        int __reserved;
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct statx
    {
        /* 0x00 */
        uint stx_mask; /* What results were written [uncond] */
        uint stx_blksize;  /* Preferred general I/O size [uncond] */
        ulong stx_attributes;   /* Flags conveying information about the file [uncond] */
        /* 0x10 */
        uint stx_nlink;    /* Number of hard links */
        uint stx_uid;  /* User ID of owner */
        uint stx_gid;  /* Group ID of owner */
        ushort stx_mode; /* File mode */
        ushort __spare0;
        /* 0x20 */
        ulong stx_ino;  /* Inode number */
        ulong stx_size; /* File size */
        ulong stx_blocks;   /* Number of 512-byte blocks allocated */
        ulong stx_attributes_mask; /* Mask to show what's supported in stx_attributes */
        /* 0x40 */
        statx_timestamp stx_atime;  /* Last access time */
        statx_timestamp stx_btime;  /* File creation time */
        statx_timestamp stx_ctime;  /* Last attribute change time */
        statx_timestamp stx_mtime;  /* Last data modification time */
        /* 0x80 */
        uint stx_rdev_major;   /* Device ID of special file [if bdev/cdev] */
        uint stx_rdev_minor;
        uint stx_dev_major;    /* ID of device containing file [uncond] */
        uint stx_dev_minor;
        /* 0x90 */
        ulong stx_mnt_id;
        ulong __spare2;
        /* 0xa0 */
        unsafe fixed ulong __spare3[12]; /* Spare space for future expansion */
        /* 0x100 */
    }

}