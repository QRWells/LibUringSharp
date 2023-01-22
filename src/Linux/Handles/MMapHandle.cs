using Microsoft.Win32.SafeHandles;

namespace Linux.Handles;

/// <summary>
///     Wrapper of a file descriptor that should be disposed of using the "munmap" syscall
/// </summary>
public class MMapHandle : SafeHandleMinusOneIsInvalid
{
    public MMapHandle(nint ptr, ulong size) : base(true)
    {
        Size = size;
        SetHandle(ptr);
    }

    public ulong Size { get; }

    public nint Address => handle;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid) return true;
        return LibC.MemUnmap(handle, Size) == 0;
    }
}