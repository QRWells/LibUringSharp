using System.Numerics;
using System.Runtime.InteropServices;
using static Linux.LibC;

namespace LibUringSharp;

/// <summary>
///     Used for <see cref="Ring.RegisterBufferRing" /> and <see cref="Ring.UnregisterBufRing" />
///     <remarks>User needs to call <see cref="Release" /> to free the memory manually</remarks>
/// </summary>
public unsafe struct BufferRing
{
    private readonly io_uring_buf_ring* _bufRing;
    public int Id { get; }
    internal nuint RingAddress => new(_bufRing);
    internal uint Entries { get; }
    private int Mask => (int)(Entries - 1);
    private int _counter = 0;
    private bool _released = false;

    public BufferRing(int id, uint entries)
    {
        entries = BitOperations.RoundUpToPowerOf2(entries);
        var size = 16 * entries;
        _bufRing = (io_uring_buf_ring*)NativeMemory.AlignedAlloc(size, 4096);
        _bufRing->tail = 0;
        Id = id;
        Entries = entries;
    }

    /// <summary>
    ///     Add a buffer to the buffer ring.
    /// </summary>
    /// <param name="addr">Address of the buffer</param>
    /// <param name="len">Length of the buffer</param>
    /// <param name="bufId">Unique buffer id</param>
    /// <param name="bufOffset">Offset from the current tail</param>
    public void Add(void* addr, uint len, ushort bufId, int bufOffset)
    {
        var bufArray = io_uring_buf_ring.bufs(_bufRing);
        var bufPtr = &bufArray[(_bufRing->tail + bufOffset) & Mask];

        bufPtr->addr = (ulong)addr;
        bufPtr->len = len;
        bufPtr->bid = bufId;

        _counter++;
    }

    /// <summary>
    ///     Commit <code>count</code> previously added buffers to the kernel.
    /// </summary>
    public void Commit()
    {
        var newTail = (ushort)(_bufRing->tail + _counter);
        Volatile.Write(ref _bufRing->tail, newTail);
    }

    public void Release()
    {
        if (_released) return;
        _released = true;
        NativeMemory.AlignedFree(_bufRing);
    }
}