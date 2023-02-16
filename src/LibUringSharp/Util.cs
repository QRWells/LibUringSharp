using QRWells.LibUringSharp.Linux.Handles;

namespace QRWells.LibUringSharp;

public static class Util
{
    internal static ulong NPages(ulong size, long pageSize)
    {
        size--;
        size /= (ulong)pageSize;
        if (size == 0) return 64;
        return 64 - ulong.LeadingZeroCount(size);
    }

    public static Span<int> ToIntSpan(this Span<FileDescriptor> files)
    {
        var result = new int[files.Length];
        for (var i = 0; i < files.Length; i++) result[i] = (int)files[i].DangerousGetHandle();

        return result;
    }
}