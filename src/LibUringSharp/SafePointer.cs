using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace QRWells.LibUringSharp;

public abstract class SafePointer : SafeHandleZeroOrMinusOneIsInvalid
{
    protected SafePointer() : base(true)
    {
    }

    public abstract override bool IsInvalid { get; }

    protected abstract override bool ReleaseHandle();

    public static implicit operator nint(SafePointer pointer)
    {
        return pointer.handle;
    }
}

/// <summary>
///     Used to wrap a pointer to a struct or class in a safe way.
/// </summary>
/// <typeparam name="T">The type of the object.</typeparam>
public sealed class FixedPointer<T> : SafePointer
{
    private GCHandle _handle;

    public FixedPointer(T value)
    {
        _handle = GCHandle.Alloc(value, GCHandleType.Pinned);
        SetHandle(_handle.AddrOfPinnedObject());
    }

    public FixedPointer(IEnumerable<T> value)
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
}

/// <summary>
///     Used to wrap a pointer to a struct or class in a safe way.
/// </summary>
/// <typeparam name="T">The type of the object.</typeparam>
public sealed class FixedArray<T> : SafePointer
{
    private GCHandle _handle;

    public FixedArray(T[] value)
    {
        _handle = GCHandle.Alloc(value, GCHandleType.Pinned);
        SetHandle(_handle.AddrOfPinnedObject());
        Length = value.Length;
    }

    public override bool IsInvalid => !_handle.IsAllocated;
    public int Length { get; }

    protected override bool ReleaseHandle()
    {
        _handle.Free();
        return true;
    }
}