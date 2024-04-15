using System.Runtime.InteropServices;

namespace QRWells.LibUringSharp.Linux;

public static partial class LibC
{
    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number, syscall_arg arg1);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4, syscall_arg arg5);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4, syscall_arg arg5, syscall_arg arg6);

    [LibraryImport(Libc, SetLastError = true)]
    public static partial syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4, syscall_arg arg5, syscall_arg arg6, syscall_arg arg7);
}