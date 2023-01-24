using System.Runtime.InteropServices;

namespace Linux;

public static partial class LibC
{
    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number);

    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number, syscall_arg arg1);

    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2);

    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3);

    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4);

    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4, syscall_arg arg5);

    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4, syscall_arg arg5, syscall_arg arg6);

    [DllImport(Libc, SetLastError = true)]
    public static extern syscall_arg syscall(syscall_arg number, syscall_arg arg1, syscall_arg arg2, syscall_arg arg3,
        syscall_arg arg4, syscall_arg arg5, syscall_arg arg6, syscall_arg arg7);
}