using System.Runtime.InteropServices;
using Linux.Handles;

namespace Linux;

public static partial class LibC
{
    public static FileDescriptor EventFd(uint init_val, EventFdFlags flags)
    {
        var fd = EventFd(init_val, (int)flags);
        if (fd < 0)
            throw new System.IO.IOException("eventfd failed");
        return new FileDescriptor(fd);
    }

    [Flags]
    public enum EventFdFlags
    {
        Semaphore = 1,
        NonBlock = 0x800,
        CloseOnExec = 0x80000,
    }


    [DllImport(Libc, EntryPoint = "eventfd", CharSet = CharSet.Ansi)]
    internal static extern int EventFd(uint initval, int flags);
}