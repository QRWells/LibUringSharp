namespace LibUringSharp.Test;

public class RingTests
{
    [Test]
    public void TestSubmitOne()
    {
        var ring = new Ring(4);

        Assert.That(ring, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(ring.IsKernelIoPolling, Is.False);
            Assert.That(ring.IsKernelSubmissionQueuePolling, Is.False);
        });

        Assert.That(ring.TryGetNextSqe(out var sub), Is.True);
        sub.PrepareNop(2023);
        Assert.That(ring.SubmitAndWait(1), Is.EqualTo(1));

        Assert.That(ring.TryGetCompletion(out var com), Is.True);
        Assert.That(com.UserData, Is.EqualTo(2023));
        Assert.That(com.Result, Is.EqualTo(0));
    }

    [Test]
    public void TestSubmitMore()
    {
        var ring = new Ring(4);

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

        ring.SubmitAndWait(4);

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
        var ring = new Ring(4);

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

        ring.SubmitAndWait(4);

        var coms = new Completion.Completion[4];

        Assert.That(ring.TryGetBatch(coms), Is.EqualTo(4));

        for (ulong i = 0; i < 4; i++)
        {
            Assert.That(coms[i].UserData, Is.EqualTo(i));
            Assert.That(coms[i].Result, Is.EqualTo(0));
        }
    }
}