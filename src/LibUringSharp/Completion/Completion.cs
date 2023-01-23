namespace LibUringSharp.Completion;

public readonly struct Completion
{
    public readonly int Result;
    public readonly ulong UserData;
    public readonly uint Flags;

    public Completion(int res, ulong userData, uint flags)
    {
        Result = res;
        UserData = userData;
        Flags = flags;
    }
}