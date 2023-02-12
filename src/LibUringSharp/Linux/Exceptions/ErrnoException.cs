namespace LibUringSharp.Linux.Exceptions;

public class ErrnoException : IOException
{
    public ErrnoException()
    {
        Errno = LibC.errno;
    }

    public int Errno { get; }
}

public enum ErrorNo
{
    PermissionDenied = 1,
    NoSuchFileOrDirectory = 2,
    NoSuchProcess = 3,
    InterruptedSystemCall = 4,
    InputOutputError = 5,
    DeviceNotConfigured = 6,
    ArgumentListTooLong = 7,
    ExecFormatError = 8,
    BadFileDescriptor = 9,
    NoChildProcesses = 10,
    ResourceTemporarilyUnavailable = 11,
    CannotAllocateMemory = 12,
    PermissionDenied2 = 13,
    BadAddress = 14,
    BlockDeviceRequired = 15,
    DeviceOrResourceBusy = 16,
    FileExists = 17,
    InvalidCrossDeviceLink = 18,
    NoSuchDevice = 19,
    NotADirectory = 20,
    IsADirectory = 21,
    InvalidArgument = 22,
    TooManyOpenFilesInSystem = 23,
    TooManyOpenFiles = 24,
    InappropriateIoControlOperation = 25,
    TextFileBusy = 26,
    FileTooLarge = 27,
    NoSpaceLeftOnDevice = 28,
    IllegalSeek = 29,
    ReadOnlyFileSystem = 30,
    TooManyLinks = 31,
    BrokenPipe = 32,
    NumericalArgumentOutOfRange = 33,
    NumericalResultNotRepresentable = 34
}