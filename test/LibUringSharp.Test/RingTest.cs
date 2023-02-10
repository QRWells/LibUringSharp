using System.Text;
using LibUringSharp.Submission;

namespace LibUringSharp.Test;

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

        Assert.That(ring.TryGetNextSubmission(out var sub), Is.True);
        sub.PrepareNop(2023);
        ring.Prepared(in sub);
        Assert.Multiple(() =>
        {
            Assert.That(ring.Submit(), Is.EqualTo(1));

            Assert.That(ring.TryGetCompletion(out var com), Is.True);
            Assert.That(com.UserData, Is.EqualTo(2023));
            Assert.That(com.Result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TestPreparePartial()
    {
        using var ring = new Ring(4);

        Assert.That(ring, Is.Not.Null);

        Assert.That(ring.TryGetNextSubmission(out var sub1), Is.True);
        Assert.That(ring.TryGetNextSubmission(out var sub2), Is.True);

        sub2.PrepareNop(2023);

        Assert.That(ring.Submit(), Is.EqualTo(0));
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
            Assert.That(ring.TryGetNextSubmission(out var sub), Is.True);
            sub.PrepareNop(i);
            ring.Prepared(in sub);
        }

        Assert.That(ring.TryGetNextSubmission(out _), Is.False);

        Assert.That(ring.Submit(), Is.EqualTo(4));

        for (ulong i = 0; i < 4; i++)
        {
            Assert.That(ring.TryGetCompletion(out var com), Is.True);
            Assert.That(com.UserData, Is.EqualTo(i));
            Assert.That(com.Result, Is.EqualTo(0));
        }
    }

    [Test]
    public void TestGetMultiple()
    {
        using var ring = new Ring(4);

        var submissions = new Submission.Submission[4];
        Assert.That(ring.GetSubmissions(submissions), Is.EqualTo(4));
        for (ulong i = 0; i < 4; i++)
        {
            submissions[i].PrepareNop(i);
            ring.Prepared(in submissions[i]);
        }

        ring.Submit();

        var completions = new Completion.Completion[4];

        Assert.That(ring.TryGetCompletions(completions), Is.EqualTo(4));

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
        var file = Open("test.txt", OpenOption.Create | OpenOption.Truncate | OpenOption.ReadWrite,
            new FilePermissions());
        var str = "Hello World!\n";

        Assert.That(ring, Is.Not.Null);
        if (!ring.TryGetNextSubmission(out var sub))
            Assert.Fail("Failed to get next submission queue entry");

        // Write to the file
        var bytes = Encoding.UTF8.GetBytes(str);
        sub.Option |= SubmissionOption.IoLink;
        sub.PrepareWrite(file, bytes, 0);
        ring.Prepared(in sub);

        Assert.That(ring.Submit(), Is.EqualTo(1));

        if (!ring.TryGetCompletion(out var com))
            Assert.Fail("Failed to get completion");
        Assert.That(com.Result, Is.EqualTo(str.Length));

        // Read the file
        var buffer = new byte[16];
        if (!ring.TryGetNextSubmission(out sub))
            Assert.Fail("Failed to get next submission queue entry");
        sub.PrepareRead(file, buffer, 0);
        ring.Prepared(in sub);

        Assert.That(ring.Submit(), Is.EqualTo(1));

        if (!ring.TryGetCompletion(out com))
            Assert.Fail("Failed to get completion");
        Assert.That(com.Result, Is.EqualTo(str.Length));
        Assert.That(Encoding.UTF8.GetString(buffer[..str.Length]), Is.EqualTo(str));

        file.Dispose();
    }

    [Test]
    public async Task TestSelectBuffer()
    {
        using var ring = new Ring(4);
        var id = await ring.RegisterBufferGroupAsync(1024, 1);
        Assert.That(id, Is.EqualTo(0));
        ring.UnregisterBufferGroup(id);
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        File.Delete("test.txt");
    }
}