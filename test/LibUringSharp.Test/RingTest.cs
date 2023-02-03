namespace LibUringSharp.Test;

using System.Text;
using LibUringSharp.Submission;
using static Linux.LibC;

public class RingTests
{
    [Test]
    public void TestSubmitOne()
    {
        using var ring = new Ring(4);

        Assert.That(ring, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ring.IsKernelIoPolling, Is.False);
            Assert.That(ring.IsKernelSubmissionQueuePolling, Is.False);
        });

        Assert.That(ring.TryGetNextSqe(out var sub), Is.True);
        sub.PrepareNop(2023);
        Assert.Multiple(() =>
        {
            Assert.That(ring.Submit(), Is.EqualTo(1));

            Assert.That(ring.TryGetCompletion(out var com), Is.True);
            Assert.That(com.UserData, Is.EqualTo(2023));
            Assert.That(com.Result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TestSubmitMore()
    {
        using var ring = new Ring(4);

        Assert.That(ring, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ring.IsKernelIoPolling, Is.False);
            Assert.That(ring.IsKernelSubmissionQueuePolling, Is.False);
        });

        for (ulong i = 0; i < 4; i++)
        {
            Assert.That(ring.TryGetNextSqe(out var sub), Is.True);
            sub.PrepareNop(i);
        }

        ring.Submit();

        for (ulong i = 0; i < 4; i++)
        {
            Assert.That(ring.TryGetCompletion(out var com), Is.True);
            Assert.That(com.UserData, Is.EqualTo(i));
            Assert.That(com.Result, Is.EqualTo(0));
        }
    }

    [Test]
    public void TestGetBatch()
    {
        using var ring = new Ring(4);

        Assert.That(ring, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ring.IsKernelIoPolling, Is.False);
            Assert.That(ring.IsKernelSubmissionQueuePolling, Is.False);
        });

        for (ulong i = 0; i < 4; i++)
        {
            Assert.That(ring.TryGetNextSqe(out var sub), Is.True);
            sub.PrepareNop(i);
        }

        ring.Submit();

        var completions = new Completion.Completion[4];

        Assert.That(ring.TryGetBatch(completions), Is.EqualTo(4));

        for (ulong i = 0; i < 4; i++)
            Assert.Multiple(() =>
            {
                Assert.That(completions[i].UserData, Is.EqualTo(i));
                Assert.That(completions[i].Result, Is.EqualTo(0));
            });
    }

    [Test]
    public void TestFileIO()
    {
        using var ring = new Ring(4);
        using var file = Open("test.txt", OpenOption.Create | OpenOption.Truncate | OpenOption.ReadWrite, new FilePermissions());
        var str = "Hello World!";

        Assert.That(ring, Is.Not.Null);
        if (!ring.TryGetNextSqe(out var sub))
            Assert.Fail("Failed to get next submission queue entry");

        // Write to the file
        var bytes = Encoding.UTF8.GetBytes(str);
        sub.PrepareWrite(file, bytes, 0);
        sub.Option |= SubmissionOption.IoLink;

        // Read the file
        var buffer = new byte[str.Length];
        if (!ring.TryGetNextSqe(out sub))
            Assert.Fail("Failed to get next submission queue entry");
        sub.PrepareRead(file, buffer, 0);


        Assert.That(ring.Submit(), Is.EqualTo(2));

        if (!ring.TryGetCompletion(out var com))
            Assert.Fail("Failed to get completion");
        Assert.That(com.Result, Is.EqualTo(str.Length));

        if (!ring.TryGetCompletion(out com))
            Assert.Fail("Failed to get completion");
        Assert.That(com.Result, Is.EqualTo(str.Length));
        Assert.That(Encoding.UTF8.GetString(buffer), Is.EqualTo(str));
    }
}