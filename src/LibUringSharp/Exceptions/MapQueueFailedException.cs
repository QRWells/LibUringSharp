namespace QRWells.LibUringSharp.Exceptions;

public class MapQueueFailedException(MapQueueFailedException.QueueType queueType) : Exception
{
    public enum QueueType
    {
        SubmissionQueue,
        SubmissionQueueEntries,
        CompletionQueue
    }

    public QueueType Type { get; } = queueType;
}