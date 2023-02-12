using Microsoft.Win32.SafeHandles;

namespace LibUringSharp.Linux.Handles;

public class FileDescriptor : SafeHandleMinusOneIsInvalid
{
    public FileDescriptor(int fd) : base(true)
    {
        SetHandle(fd);
    }

    public override bool IsInvalid => handle.CompareTo(nint.Zero) < 0;

    protected override bool ReleaseHandle()
    {
        if (IsInvalid) return true;
        return LibC.Close(handle.ToInt32()) == 0;
    }

    public static implicit operator int(FileDescriptor handle)
    {
        return handle.handle.ToInt32();
    }

    public static implicit operator uint(FileDescriptor handle)
    {
        return (uint)handle.handle.ToInt32();
    }

    public static implicit operator FileDescriptor(int fd)
    {
        return new FileDescriptor(fd);
    }
}