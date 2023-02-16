namespace QRWells.LibUringSharp;

public readonly struct KernelVersion : IComparable<KernelVersion>
{
    private readonly int _major;
    private readonly int _minor;
    private readonly int _patch;

    public KernelVersion()
    {
        var version = Environment.OSVersion.Version;
        _major = version.Major;
        _minor = version.Minor;
        _patch = version.Build;
    }

    public static bool IsAtLeast(int major, int minor)
    {
        return new KernelVersion().AtLeast(major, minor);
    }
    
    public static bool IsAtLeast(int major, int minor, int patch)
    {
        return new KernelVersion().AtLeast(major, minor, patch);
    }

    public bool AtLeast(int major, int minor)
    {
        return _major > major
               || (_major == major && _minor >= minor);
    }

    public bool AtLeast(int major, int minor, int patch)
    {
        return _major > major
               || (_major == major && _minor > minor)
               || (_major == major && _minor == minor && _patch >= patch);
    }

    public int CompareTo(KernelVersion other)
    {
        var majorComparison = _major.CompareTo(other._major);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = _minor.CompareTo(other._minor);
        if (minorComparison != 0) return minorComparison;

        return _patch.CompareTo(other._patch);
    }
}