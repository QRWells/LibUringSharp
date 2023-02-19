namespace QRWells.LibUringSharp.Exceptions;

public class MapQueueFailedException : Exception
{
    public enum QueueType
    {
        SubmissionQueue,
        SubmissionQueueEntries,
        CompletionQueue
    }

    public MapQueueFailedException(QueueType queueType)
    {
        Type = queueType;
    }

    public QueueType Type { get; }
}