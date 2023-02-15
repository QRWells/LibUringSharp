using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace LibUringSharp;

public abstract class SafePointer : SafeHandleZeroOrMinusOneIsInvalid
{
    protected SafePointer() : base(true)
    {
    }

    public abstract override bool IsInvalid { get; }

    protected abstract override bool ReleaseHandle();
}

/// <summary>
///     Used to wrap a pointer to a struct or class in a safe way.
/// </summary>
/// <typeparam name="T">The type of the object.</typeparam>
public sealed class FixedPointer<T> : SafePointer
{
    private GCHandle _handle;

    public FixedPointer(ref T value)
    {
        _handle = GCHandle.Alloc(value, GCHandleType.Pinned);
        SetHandle(_handle.AddrOfPinnedObject());
    }

    public override bool IsInvalid => !_handle.IsAllocated;

    protected override bool ReleaseHandle()
    {
        _handle.Free();
        return true;
    }

    public static implicit operator nint(FixedPointer<T> pointer)
    {
        return pointer.handle;
    }
}