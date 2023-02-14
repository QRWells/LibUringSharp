using System.Runtime.InteropServices;

namespace LibUringSharp;

/// <summary>
///     Used to wrap a pointer to a struct or class in a safe way.
/// </summary>
/// <typeparam name="T">The type of the object.</typeparam>
public sealed class SafePointer<T> : IDisposable
{
    private GCHandle _handle;
    private nint _ptr = nint.Zero;

    public SafePointer(T ptr)
    {
        if (null == ptr) return;
        _handle = GCHandle.Alloc(ptr, GCHandleType.Pinned);
        _ptr = _handle.AddrOfPinnedObject();
    }

    public bool IsDisposed { get; private set; }

    public unsafe void* Pointer => (void*)_ptr;

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        if (!_handle.IsAllocated) return;
        _handle.Free();
        _ptr = nint.Zero;

        GC.SuppressFinalize(this);
    }

    ~SafePointer()
    {
        Dispose();
    }

    public static implicit operator nint(SafePointer<T> ptr)
    {
        return ptr._ptr;
    }
}