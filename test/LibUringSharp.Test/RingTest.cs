namespace LibUringSharp.Test;

public class RingTests
{
    [Test]
    public void Test1()
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
        Assert.AreEqual(ring.SubmitAndWait(1), 1);

        Assert.That(ring.TryGetCompletion(out var com), Is.True);
        Assert.AreEqual(0, com.Result);
        Assert.AreEqual(2023, com.UserData);
    }
}