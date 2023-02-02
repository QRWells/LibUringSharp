using System.Runtime.InteropServices;
using Linux.Handles;

namespace Linux;

public static partial class LibC
{
    [Flags]
    public enum OpenOption
    {
        ReadOnly = 0b00,
        WriteOnly = 0b01,
        ReadWrite = 0b10,
        AccessModeMask = 0b11,

        Create = 0x40,
        Exclusive = 0x80,
        Truncate = 0x200,
        Append = 0x400,
        NonBlock = 0x800,
        Sync = 0x1000,
        Async = 0x2000,

        Direct = 0x4000,
        LargeFile = 0x8000,
        Directory = 0x10000,
        NoFollow = 0x20000,
        NoATime = 0x40000,
        CloseOnExec = 0x80000
    }

    /// <summary>
    ///     Open and possibly create a file.
    /// </summary>
    /// <param name="path">Path to the file</param>
    /// <param name="flags">Options for opening the file, must indicates access mode</param>
    /// <param name="mode">File permission mode, valid only when <see cref="OpenOption.Create" /> is specified</param>
    /// <returns>File descriptor for the file</returns>
    /// <exception cref="IOException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static FileDescriptor Open(string path, OpenOption flags, FilePermissions mode)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentNullException(nameof(path));
        var fd = Open(path, (int)flags, mode);
        if (fd == -1) throw new IOException("open failed");

        return new FileDescriptor(fd);
    }

    [DllImport(Libc, EntryPoint = "open", CharSet = CharSet.Ansi)]
    public static extern int Open(string path, int flags, int mode);

    [DllImport(Libc, EntryPoint = "close")]
    public static extern int Close(int fd);

    [DllImport(Libc, EntryPoint = "stat", CharSet = CharSet.Ansi)]
    public static extern int Stat(string path, out stat stat);

    public class FilePermissions
    {
        [Flags]
        public enum Permission
        {
            Read = 0b100,
            Write = 0b010,
            Execute = 0b001
        }

        private const int OwnerMask = 0b111 << 6;
        private const int GroupMask = 0b111 << 3;
        private const int OtherMask = 0b111 << 0;
        private readonly int _value;

        private FilePermissions(int value)
        {
            _value = value & (OwnerMask | GroupMask | OtherMask);
        }

        public FilePermissions() : this(0x1ed)
        {
        }

        public FilePermissions(Permission owner, Permission group, Permission other)
        {
            _value = (int)owner | (int)group | (int)other;
        }

        public Permission Owner => (Permission)((_value & OwnerMask) >> 6);
        public Permission Group => (Permission)((_value & GroupMask) >> 3);
        public Permission Other => (Permission)((_value & OtherMask) >> 0);

        public bool OwnerCanRead => Owner.HasFlag(Permission.Read);
        public bool OwnerCanWrite => Owner.HasFlag(Permission.Write);
        public bool OwnerCanExecute => Owner.HasFlag(Permission.Execute);

        public bool GroupCanRead => Group.HasFlag(Permission.Read);
        public bool GroupCanWrite => Group.HasFlag(Permission.Write);
        public bool GroupCanExecute => Group.HasFlag(Permission.Execute);

        public bool OtherCanRead => Other.HasFlag(Permission.Read);
        public bool OtherCanWrite => Other.HasFlag(Permission.Write);
        public bool OtherCanExecute => Other.HasFlag(Permission.Execute);

        public static FilePermissions FromInt32(int value)
        {
            return new FilePermissions(value);
        }

        public static implicit operator int(FilePermissions permissions)
        {
            return permissions._value;
        }

        public static implicit operator FilePermissions(int value)
        {
            return new FilePermissions(value);
        }

        public override string ToString()
        {
            return $"Owner: {Owner}, Group: {Group}, Other: {Other}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct iovec
    {
        // void* iov_base;
        public nint iov_base;

        // size_t iov_len;
        public ulong iov_len;
    }
}