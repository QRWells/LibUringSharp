using System.Runtime.InteropServices;

namespace Linux;

public static partial class LibC
{
    [Flags]
    public enum EPOLL_EVENTS
    {
        EPOLLIN = 0x001,
        EPOLLPRI = 0x002,
        EPOLLOUT = 0x004,
        EPOLLRDNORM = 0x040,
        EPOLLRDBAND = 0x080,
        EPOLLWRNORM = 0x100,
        EPOLLWRBAND = 0x200,
        EPOLLMSG = 0x400,
        EPOLLERR = 0x008,
        EPOLLHUP = 0x010,
        EPOLLRDHUP = 0x2000,
        EPOLLEXCLUSIVE = 1 << 28,
        EPOLLWAKEUP = 1 << 29,
        EPOLLONESHOT = 1 << 30,
        EPOLLET = 1 << 31
    }

    /* Valid opcodes ( "op" parameter ) to issue to epoll_ctl().  */
    private const int EPOLL_CTL_ADD = 1; /* Add a file descriptor to the interface.  */
    private const int EPOLL_CTL_DEL = 2; /* Remove a file descriptor from the interface.  */
    private const int EPOLL_CTL_MOD = 3; /* Change file descriptor epoll_event structure.  */

    [DllImport(Libc, SetLastError = true)]
    public static extern int epoll_create(int size);

    [DllImport(Libc, SetLastError = true)]
    public static extern int epoll_create1(int flags);

    [DllImport(Libc, SetLastError = true)]
    public static extern int epoll_ctl(int epfd, int op, int fd, ref epoll_event ev);

    [DllImport(Libc, SetLastError = true)]
    public static extern unsafe int epoll_wait(int epfd, epoll_event* events, int maxevents, int timeout);

    [DllImport(Libc, SetLastError = true)]
    public static extern unsafe int epoll_pwait(int epfd, epoll_event* events, int maxevents, int timeout,
        sigset_t* sigmask);

    [DllImport(Libc, SetLastError = true)]
    public static extern unsafe int
        epoll_pwait(int epfd, epoll_event* events, int maxevents, int timeout, nint sigmask);

    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public unsafe struct epoll_data_t
    {
        [FieldOffset(0)] public void* ptr;
        [FieldOffset(0)] public int fd;
        [FieldOffset(0)] public uint u32;
        [FieldOffset(0)] public ulong u64;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct epoll_event
    {
        public uint events; /* Epoll events */
        public epoll_data_t data; /* User data variable */
    }
}