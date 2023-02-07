using System.Runtime.InteropServices;
using static System.Numerics.BitOperations;

namespace LibUringSharp;

public unsafe readonly struct BufferGroup
{
    private readonly void* _bufferBase;
    private readonly uint _bufferSize;
    private readonly uint _bufferCount;

    public uint TotalSize => _bufferSize * _bufferCount;
    public uint BufferSize => _bufferSize;
    public uint BufferCount => _bufferCount;

    internal void* GetBuffer(uint index)
    {
        if (index >= _bufferCount)
            throw new ArgumentOutOfRangeException(nameof(index), "index must be less than buffer count");

        return (byte*)_bufferBase + (index * _bufferSize);
    }

    internal void* Base => _bufferBase;

    public BufferGroup(uint bufferSize, uint bufferCount)
    {
        if (bufferSize == 0)
            throw new ArgumentException("buffer size must be greater than 0", nameof(bufferSize));

        if (bufferCount == 0)
            throw new ArgumentException("buffer count must be greater than 0", nameof(bufferCount));

        bufferSize = RoundUpToPowerOf2(bufferSize);
        bufferCount = RoundUpToPowerOf2(bufferCount);

        _bufferBase = NativeMemory.AlignedAlloc(bufferSize * bufferCount, 8);
        _bufferSize = bufferSize;
        _bufferCount = bufferCount;
    }

    public void Dispose()
    {
        if (_bufferBase != null)
            NativeMemory.AlignedFree(_bufferBase);
    }
}