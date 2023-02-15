using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LibUringSharp.Buffer;
using SafeBuffer = LibUringSharp.Buffer.SafeBuffer;

namespace LibUringSharp.Test;

[Platform("Linux")]
public class BufferRingTest
{
    private const int BufferSize = 1024;
    private const int BufferCount = 16;
    private const int TotalSize = BufferSize * BufferCount;

    [OneTimeSetUp]
    public void Setup()
    {
        if (!KernelVersion.IsAtLeast(5, 19))
            Assert.Ignore("Buffer ring is only supported on Linux 5.19 and above");

        var file = File.Open("test.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write);
        var buffer = new byte[TotalSize];
        for (var i = 0; i < BufferCount; i++) Array.Fill(buffer, (byte)(i + 1), i * BufferSize, BufferSize);

        file.Write(buffer);
        file.Close();
    }

    [Test]
    public void BufferRingFuncTest()
    {
        using var ring = new Ring(BufferCount);
        using var file = File.Open("test.txt", FileMode.Open, FileAccess.Read, FileShare.Read);
        var fd = file.SafeFileHandle.DangerousGetHandle().ToInt32();
        var bufferRing = new BufferRing(1, BufferCount);
        ring.RegisterBufferRing(bufferRing);

        var buffers = new SafeBuffer[BufferCount];

        for (var i = 0; i < BufferCount; i++)
        {
            buffers[i] = SafeBuffer.Create(BufferSize);
            bufferRing.Add(buffers[i], (ushort)(i + 1), i);
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
            Assert.That(ring.TryGetCompletion(out var comp), Is.True);
            Assert.That(comp.IsBuffered, Is.True);
            Assert.That(comp.Result, Is.EqualTo(BufferSize));
            Assert.That(comp.UserData, Is.EqualTo(buffers[comp.BufferId - 1][0]));
        }

        for (var i = 0; i < BufferCount; i++)
        {
            buffers[i].Dispose();
        }
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        File.Delete("test.txt");
    }
}