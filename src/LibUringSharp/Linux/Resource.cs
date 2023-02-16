using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace QRWells.LibUringSharp.Linux;

public static partial class LibC
{
    public enum ResourceLimit
    {
        /// <summary>
        ///     Per-process CPU limit, in seconds.
        /// </summary>
        Cpu = 0,

        /// <summary>
        ///     Largest file that can be created, in bytes.
        /// </summary>
        FileSize = 1,

        /// <summary>
        ///     Maximum size of data segment, in bytes.
        /// </summary>
        Data = 2,

        /// <summary>
        ///     Maximum size of stack segment, in bytes.
        /// </summary>
        Stack = 3,

        /// <summary>
        ///     Largest core file that can be created, in bytes.
        /// </summary>
        Core = 4,

        /// <summary>
        ///     Largest resident set size, in bytes.
        ///     This affects swapping; processes that are
        ///     exceeding their resident set size will be
        ///     more likely to have physical memory taken from them.
        /// </summary>
        ResidentSet = 5,

        /// <summary>
        ///     Number of processes.
        /// </summary>
        Process = 6,

        /// <summary>
        ///     Number of open files.
        /// </summary>
        Files = 7,

        /// <summary>
        ///     Locked-in-memory address space.
        /// </summary>
        MemoryLock = 8,

        /// <summary>
        ///     Address space limit.
        /// </summary>
        AddressSpace = 9,

        /// <summary>
        ///     Maximum number of file locks.
        /// </summary>
        Locks = 10,

        /// <summary>
        ///     Maximum number of pending signals.
        /// </summary>
        SignalPending = 11,

        /// <summary>
        ///     Maximum bytes in POSIX message queues.
        /// </summary>
        MessageQueue = 12,

        /// <summary>
        ///     Maximum nice priority allowed to raise to.
        ///     Nice levels 19 .. -20 correspond to 0 .. 39 values
        ///     of this resource limit.
        /// </summary>
        Nice = 13,

        /// <summary>
        ///     Maximum realtime priority allowed for non-priviledged
        /// </summary>
        RealTimePriority = 14,

        /// <summary>
        ///     Maximum CPU time in microseconds that a process scheduled under a real-time
        ///     scheduling policy may consume without making a blocking system call before
        ///     being forcibly descheduled.
        /// </summary>
        RealTimeTime = 15,

        Limits = 16
    }

    private const int NrGetRLimit = 163;
    private const int NrSetRLimit = 164;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int GetRLimit(ResourceLimit resource, RLimit* rlim)
    {
        return (int)syscall(NrGetRLimit, (int)resource, rlim);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe int SetRLimit(ResourceLimit resource, RLimit* rlim)
    {
        return (int)syscall(NrSetRLimit, (int)resource, rlim);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RLimit
    {
        /* The current (soft) limit.  */
        public ulong cur;

        /* The hard limit.  */
        public ulong max;
    }
}