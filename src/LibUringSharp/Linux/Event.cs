using System.Runtime.InteropServices;
using QRWells.LibUringSharp.Linux.Handles;

namespace QRWells.LibUringSharp.Linux;

public static partial class LibC
{
    [Flags]
    public enum EventFdFlags
    {
        Semaphore = 1,
        NonBlock = 0x800,
        CloseOnExec = 0x80000
    }

    public static FileDescriptor EventFd(uint init_val, EventFdFlags flags)
    {
        var fd = EventFd(init_val, (int)flags);
        if (fd < 0)
            throw new IOException("eventfd failed");
        return new FileDescriptor(fd);
    }


    [DllImport(Libc, EntryPoint = "eventfd", CharSet = CharSet.Ansi)]
    internal static extern int EventFd(uint initval, int flags);
}