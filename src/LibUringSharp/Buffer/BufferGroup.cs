using System.Runtime.InteropServices;
using static System.Numerics.BitOperations;

namespace LibUringSharp.Buffer;

/// <summary>
///     Used for <see cref="Submission.Submission.PrepareProvideBuffers" /> and
///     <see cref="Submission.Submission.PrepareRemoveBuffers" />
///     <remarks>User needs to call <see cref="Release" /> to free the memory manually</remarks>
/// </summary>
internal readonly unsafe struct BufferGroup
{
    public uint TotalSize => BufferSize * BufferCount;
    public uint BufferSize { get; }
    public uint BufferCount { get; }

    internal void* GetBuffer(uint index)
    {
        if (index >= BufferCount)
            throw new ArgumentOutOfRangeException(nameof(index), "index must be less than buffer count");

        return (byte*)Base + index * BufferSize;
    }

    internal void* Base { get; }

    public BufferGroup(uint bufferSize, uint bufferCount)
    {
        if (bufferSize == 0)
            throw new ArgumentException("buffer size must be greater than 0", nameof(bufferSize));

        if (bufferCount == 0)
            throw new ArgumentException("buffer count must be greater than 0", nameof(bufferCount));

        bufferSize = RoundUpToPowerOf2(bufferSize);
        bufferCount = RoundUpToPowerOf2(bufferCount);

        Base = NativeMemory.AlignedAlloc(bufferSize * bufferCount, 4096);
        BufferSize = bufferSize;
        BufferCount = bufferCount;
    }

    public void Release()
    {
        if (Base != null)
            NativeMemory.AlignedFree(Base);
    }
}