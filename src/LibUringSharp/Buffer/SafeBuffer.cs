using System.Runtime.InteropServices;

namespace LibUringSharp.Buffer;

public sealed class SafeBuffer : IDisposable
{
    private bool _disposed;
    private GCHandle _handle;
    private nint _ptr;

    public SafeBuffer(byte[] buffer)
    {
        _handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        _ptr = _handle.AddrOfPinnedObject();
        Length = buffer.Length;
    }

    private unsafe SafeBuffer(void* ptr, nuint length)
    {
        _handle = default;
        _ptr = (nint)ptr;
        Length = (int)length;
    }

    public byte this[int index]
    {
        get => ToSpan()[index];
        set => ToSpan()[index] = value;
    }

    public unsafe void* Pointer => _handle.IsAllocated ? _handle.AddrOfPinnedObject().ToPointer() : (void*)_ptr;
    public int Length { get; }

    public void Dispose()
    {
        if (_disposed) return;
        if (!_handle.IsAllocated)
        {
            if (_ptr != nint.Zero) // made by Create
            {
                unsafe
                {
                    NativeMemory.AlignedFree((void*)_ptr);
                }
            }
            else // uninitialized
            {
                return;
            }
        }
        else // made by ctor
        {
            _handle.Free();
        }
        _ptr = nint.Zero;
        _disposed = true;
    }

    public Span<byte> ToSpan()
    {
        unsafe
        {
            return new Span<byte>((void*)_ptr, Length);
        }
    }

    /// <summary>
    ///     Creates a new <see cref="SafeBuffer" /> with the given length and aligns it to 4096 bytes.
    /// </summary>
    /// <param name="length">Length of the buffer.</param>
    /// <returns>A new <see cref="SafeBuffer" />.</returns>
    public static SafeBuffer Create(nuint length)
    {
        if (length == 0) throw new ArgumentOutOfRangeException(nameof(length));
        SafeBuffer res;
        unsafe
        {
            var ptr = NativeMemory.AlignedAlloc(length, 4096);
            res = new SafeBuffer(ptr, length);
        }

        return res;
    }
}