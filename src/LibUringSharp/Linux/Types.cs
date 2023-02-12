using System.Runtime.InteropServices;

namespace LibUringSharp.Linux;

public static partial class LibC
{
    private const string Libc = "libc.so.6";

    public static int errno => Marshal.GetLastWin32Error();

    public readonly struct syscall_arg
    {
        private readonly long _value;

        private unsafe syscall_arg(ulong value)
        {
            _value = *(long*)&value;
        }

        private syscall_arg(long value)
        {
            _value = value;
        }

        public static implicit operator syscall_arg(ulong arg)
        {
            return new syscall_arg(arg);
        }

        public static implicit operator syscall_arg(long arg)
        {
            return new syscall_arg(arg);
        }

        public static implicit operator syscall_arg(uint arg)
        {
            return new syscall_arg((ulong)arg);
        }

        public static implicit operator syscall_arg(int arg)
        {
            return new syscall_arg(arg);
        }

        public static unsafe implicit operator syscall_arg(void* arg)
        {
            return new syscall_arg((ulong)arg);
        }

        public static implicit operator long(syscall_arg arg)
        {
            return arg._value;
        }

        public static explicit operator int(syscall_arg arg)
        {
            return (int)arg._value;
        }

        public override string ToString()
        {
            return _value.ToString();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct sigset_t
    {
        [FieldOffset(0)] public readonly ulong __align;
        [FieldOffset(0)] public unsafe fixed byte __data[128];
    }

    public struct __kernel_timespec
    {
        public long tv_sec; /* seconds */
        public long tv_nsec; /* nanoseconds */
    }

    public struct TimeSpec
    {
        public long tv_sec;
        public long tv_nsec;
    }

    [StructLayout(LayoutKind.Sequential)]
#pragma warning disable CS8981
    public struct stat
#pragma warning restore CS8981
    {
        public ulong st_dev;
        public ulong st_ino;
        public ulong st_nlink;
        public uint st_mode;
        public uint st_uid;
        public uint st_gid;
        public int __pad0;
        public ulong st_rdev;
        public long st_size;
        public long st_blksize;
        public long st_blocks;
        public TimeSpec st_atim;
        public TimeSpec st_mtim;
        public TimeSpec st_ctim;
        public long __unused4;
        public long __unused5;
        public long __unused6;
    }
}