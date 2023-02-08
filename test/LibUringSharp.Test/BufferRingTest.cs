using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LibUringSharp.Test;

public class BufferRingTest
{
    private const int BufferSize = 1024;
    private const int BufferCount = 16;
    private const int TotalSize = BufferSize * BufferCount;

    [SetUp]
    public void Setup()
    {
        using var file = File.Open("test.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        file.SetLength(0);
        var buffer = new byte[TotalSize];
        for (var i = 0; i < BufferCount; i++) Array.Fill(buffer, (byte)(i + 1), i * BufferSize, BufferSize);

        file.Write(buffer);
    }

    [Test]
    public void BufferRingFuncTest()
    {
        using var ring = new Ring(BufferCount);
        using var file = File.Open("test.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
        var fd = file.SafeFileHandle.DangerousGetHandle().ToInt32();
        var bufferRing = new BufferRing(1, BufferCount);
        ring.RegisterBufRing(ref bufferRing);
        nint bufPtr;
        Span<byte> buffer;
        unsafe
        {
            var buf = NativeMemory.AlignedAlloc(TotalSize, BufferSize);
            bufPtr = new nint(buf);
            buffer = new Span<byte>(buf, TotalSize);
            for (var i = 0; i < BufferCount; i++)
                bufferRing.Add(Unsafe.Add<byte>(buf, i * BufferSize), BufferSize, (ushort)(i + 1), i);
        }

        bufferRing.Commit();

        for (var i = 0; i < BufferCount; i++)
        {
            ring.TryGetNextSubmission(out var sub);
            sub.PrepareSelectRead(fd, 1, BufferSize, (ulong)(i * BufferSize));
            sub.UserData = (ulong)(i + 1);
            ring.Prepared(sub);
        }

        Assert.That(ring.Submit(), Is.EqualTo(BufferCount));

        for (var i = 0; i < BufferCount; i++)
        {
            ring.TryGetCompletion(out var comp);
            Assert.That(comp.IsBuffered, Is.True);
            Assert.That(comp.Result, Is.EqualTo(BufferSize));
            Assert.That(comp.UserData, Is.EqualTo(buffer[(comp.BufferId - 1) * BufferSize]));
        }

        unsafe
        {
            NativeMemory.AlignedFree(bufPtr.ToPointer());
        }

        bufferRing.Release();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        File.Delete("test.txt");
    }
}