namespace QRWells.LibUringSharp.Completion;

public readonly struct Completion
{
    public bool IsBuffered => _flags.HasFlag(CompletionFlag.Buffer);
    public bool HasMore => _flags.HasFlag(CompletionFlag.More);
    public bool IssSocketNonEmpty => _flags.HasFlag(CompletionFlag.SocketNonEmpty);
    public bool IsNotify => _flags.HasFlag(CompletionFlag.Notify);

    public int BufferId => (int)((uint)_flags >> BufferIdShift);
    public nuint Pointer => new(UserData);

    public Completion(int res, ulong userData, uint flags)
    {
        Result = res;
        UserData = userData;
        _flags = (CompletionFlag)flags;
    }

    public readonly int Result;
    public readonly ulong UserData;
    private readonly CompletionFlag _flags;
    private const int BufferIdShift = 16;
}

[Flags]
public enum CompletionFlag : uint
{
    /// <summary>
    ///     If set, the upper 16 bits are the buffer ID.
    /// </summary>
    Buffer = 1 << 0,

    /// <summary>
    ///     If set, parent SQE will generate more CQE entries.
    /// </summary>
    More = 1 << 1,

    /// <summary>
    ///     If set, more data to read after socket recv.
    /// </summary>
    SocketNonEmpty = 1 << 2,

    /// <summary>
    ///     Set for notification CQEs. Can be used to distinct them from sends.
    /// </summary>
    Notify = 1 << 3
}