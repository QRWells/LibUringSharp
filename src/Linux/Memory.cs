using System.Runtime.InteropServices;
using Linux.Handles;

namespace Linux;

public static partial class LibC
{
    [Flags]
    public enum MemoryFlags
    {
        Shared = 0x01,
        Private = 0x02,
        Fixed = 0x10,
        Populate = 0x8000
    }

    [Flags]
    public enum MemoryProtection
    {
        None = 0x0,
        Read = 0x1,
        Write = 0x2,
        Exec = 0x4,
        GrowsDown = 0x01000000,
        GrowsUp = 0x02000000
    }

    public const uint PageSize = 4096;

    public const int MADV_DONTFORK = 10;

    public static MMapHandle MemoryMap(
        ulong length, MemoryProtection protection, MemoryFlags flags, FileDescriptor fd, long offset)
    {
        var result = MemMap(nint.Zero, length, (int)protection, (int)flags, fd, offset);
        return new MMapHandle(result, length);
    }

    [DllImport(Libc, EntryPoint = "mmap", SetLastError = true)]
    public static extern nint MemMap(nint addr, ulong length, int prot, int flags, int fd, long offset);

    [DllImport(Libc, EntryPoint = "munmap", SetLastError = true)]
    public static extern int MemUnmap(nint addr, ulong length);

    [DllImport(Libc, EntryPoint = "madvise", SetLastError = true)]
    public static extern int MemAdvise(nint addr, ulong length, int advice);
}