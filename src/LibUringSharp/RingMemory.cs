using System.Runtime.InteropServices;
using QRWells.LibUringSharp.Enums;

namespace QRWells.LibUringSharp;

public sealed class RingMemory : SafePointer
{
    private readonly ulong _ringBytes;
    private readonly ulong _sqeBytes;

    private RingMemory(ulong totalBytes, ulong sqeBytes, ulong ringBytes)
    {
        _sqeBytes = sqeBytes;
        _ringBytes = ringBytes;
        unsafe
        {
            var ptr = NativeMemory.AlignedAlloc((nuint)totalBytes, Linux.LibC.PageSize);
            if (ptr != null)
                new Span<byte>(ptr, checked((int)totalBytes)).Clear();

            SetHandle((nint)ptr);
        }

        Size = totalBytes;
    }

    public override bool IsInvalid => handle == nint.Zero;

    public ulong Size { get; }

    internal ulong RingBytes => _ringBytes;
    internal ulong SqeBytes => _sqeBytes;
    internal nint SqesAddress => handle;
    internal nint RingsAddress => handle + (int)_sqeBytes;

    public static RingMemory Allocate(uint entries, RingSetup flags = RingSetup.SetupNoMMap)
    {
        var layout = Ring.GetRequiredMemoryLayout(entries, flags | RingSetup.SetupNoMMap);
        return new RingMemory(layout.TotalBytes, layout.SqeBytes, layout.RingBytes);
    }

    protected override bool ReleaseHandle()
    {
        if (handle == nint.Zero)
            return true;

        unsafe
        {
            NativeMemory.AlignedFree((void*)handle);
        }

        handle = nint.Zero;
        return true;
    }
}
