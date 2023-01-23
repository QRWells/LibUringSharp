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
        Assert.Equals(ring.Submit(), 1);

        Assert.That(ring.TryGetCompletion(out var com), Is.True);
        Assert.Equals(0, com.Result);
        Assert.Equals(2023, com.UserData);
    }
}