namespace LibUringSharp.Enums;

[Flags]
public enum RingFeature : uint
{
    SingleMMap = 1 << 0,
    NoDrop = 1 << 1,
    SubmitStable = 1 << 2,
    RwCurPos = 1 << 3,
    CurPersonality = 1 << 4,
    FastPoll = 1 << 5,
    Poll32Bits = 1 << 6,
    SqPollNonFixed = 1 << 7,
    ExtArg = 1 << 8,
    NativeWorkers = 1 << 9,
    RSrcTags = 1 << 10,
    CqeSkip = 1 << 11,
    LinkedFile = 1 << 12
}