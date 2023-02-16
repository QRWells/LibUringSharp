namespace QRWells.LibUringSharp.Exceptions;

public class MapQueueFailedException : Exception
{
    public enum QueueType
    {
        Submission,
        Completion
    }

    public MapQueueFailedException(QueueType queueType)
    {
        Type = queueType;
    }

    public QueueType Type { get; }
}