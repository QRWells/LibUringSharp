namespace LibUringSharp;

public static class Util
{
    internal static int Fls(ulong x)
    {
        if (x == 0)
            return 0;
        return (int)(64 - ulong.LeadingZeroCount(x));
    }

    internal static uint RoundUpPow2(uint depth)
    {
        return 1U << Fls(depth - 1);
    }

    internal static ulong NPages(ulong size, long pageSize)
    {
        size--;
        size /= (ulong)pageSize;
        return (ulong)Fls((ulong)(int)size);
    }
}