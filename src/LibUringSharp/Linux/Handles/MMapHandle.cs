using Microsoft.Win32.SafeHandles;

namespace QRWells.LibUringSharp.Linux.Handles;

/// <summary>
///     Wrapper of a file descriptor that should be disposed of using the "munmap" syscall
/// </summary>
public class MMapHandle : SafeHandleMinusOneIsInvalid
{
    private readonly bool _ownsHandle;

    public MMapHandle(nint ptr, ulong size, bool ownsHandle = true) : base(ownsHandle)
    {
        _ownsHandle = ownsHandle;
        Size = size;
        SetHandle(ptr);
    }

    public ulong Size { get; }

    public nint Address => handle;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid) return true;
        if (!_ownsHandle) return true;
        return LibC.MemUnmap(handle, Size) == 0;
    }
}
