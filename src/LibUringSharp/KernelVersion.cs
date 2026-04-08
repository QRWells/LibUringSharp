using System.Runtime.InteropServices;
using System.Text;

namespace QRWells.LibUringSharp;

public readonly partial struct KernelVersion : IComparable<KernelVersion>
{
    private readonly int _major;
    private readonly int _minor;
    private readonly int _patch;

    public KernelVersion()
    {
        if (!TryReadCurrent(out var version))
        {
            var fallback = Environment.OSVersion.Version;
            version = new KernelVersion(fallback.Major, fallback.Minor, Math.Max(fallback.Build, 0));
        }

        _major = version._major;
        _minor = version._minor;
        _patch = version._patch;
    }

    private KernelVersion(int major, int minor, int patch)
    {
        _major = major;
        _minor = minor;
        _patch = patch;
    }

    public static bool IsAtLeast(int major, int minor, int patch = 0)
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

    public override string ToString()
    {
        return $"{_major}.{_minor}.{_patch}";
    }

    private static bool TryReadCurrent(out KernelVersion version)
    {
        version = default;
        if (uname(out var utsName) != 0)
            return false;

        var release = utsName.GetRelease();
        var versionText = release.Split('-', 2, StringSplitOptions.TrimEntries)[0];
        var parts = versionText.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return false;

        if (!int.TryParse(parts[0], out var major) || !int.TryParse(parts[1], out var minor))
            return false;

        var patch = 0;
        if (parts.Length > 2)
            int.TryParse(parts[2], out patch);

        version = new KernelVersion(major, minor, patch);
        return true;
    }

    [LibraryImport("libc", EntryPoint = "uname", SetLastError = true)]
    private static partial int uname(out UtsName name);

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct UtsName
    {
        private const int FieldLength = 65;

        private fixed byte _sysname[FieldLength];
        private fixed byte _nodename[FieldLength];
        private fixed byte _release[FieldLength];
        private fixed byte _version[FieldLength];
        private fixed byte _machine[FieldLength];
        private fixed byte _domainname[FieldLength];

        public string GetRelease()
        {
            fixed (byte* releasePtr = _release)
            {
                var length = 0;
                while (length < FieldLength && releasePtr[length] != 0)
                    length++;

                return Encoding.ASCII.GetString(releasePtr, length);
            }
        }
    }
}
