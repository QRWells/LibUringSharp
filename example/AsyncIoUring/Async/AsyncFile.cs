using QRWells.LibUringSharp.Linux;
using QRWells.LibUringSharp.Linux.Handles;
using static QRWells.LibUringSharp.Linux.LibC;

namespace QRWells.AsyncIoUring.Async;

public class AsyncFile : IDisposable
{
    private readonly FileDescriptor _handle;
    private bool _disposed;
    public AsyncFile(FileDescriptor handle)
    {
        _handle = handle;
    }

    public static AsyncFile Open(string path, OpenOption flags, FilePermissions mode)
    {
        var handle = LibC.Open(path, flags, mode);
        return new AsyncFile(handle);
    }

    public async Task<int> ReadAsync(byte[] buffer, int count, ulong offset)
    {
        return await IoUring.GLOBAL_RING.ReadAsync(_handle, buffer, count, offset);
    }

    public async Task<int> WriteAsync(byte[] buffer, int count, ulong offset)
    {
        return await IoUring.GLOBAL_RING.WriteAsync(_handle, buffer, count, offset);
    }

    public void Close()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _handle.Dispose();
        _disposed = true;
    }
}