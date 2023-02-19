using System.Collections.Concurrent;
using QRWells.LibUringSharp;
using QRWells.LibUringSharp.Linux.Handles;
using QRWells.LibUringSharp.Submission;

namespace QRWells.AsyncIoUring.Async;

public class IoUring : IDisposable
{
    public static IoUring GLOBAL_RING = new IoUring(128);
    private readonly Ring _ring;
    private volatile bool _running = true;
    private ulong _nextId = 0;
    private readonly ConcurrentDictionary<ulong, TaskCompletionSource<object>> _pendingTasks = new();
    private readonly ConcurrentDictionary<ulong, FixedArray<byte>> _fixedArrays = new();
    private Thread _completionThread;
    public IoUring(uint queueDepth)
    {
        _ring = new LibUringSharp.Ring(queueDepth);
        _completionThread = new Thread(CompletionThread);
        _completionThread.IsBackground = true;
        _completionThread.Name = "Completion Thread";
        _completionThread.Start();
    }

    private void CompletionThread()
    {
        while (_running)
        {
            if (_ring.TryGetCompletion(out var completion))
            {
                if (_pendingTasks.TryRemove(completion.UserData, out var tcs))
                {
                    if (_fixedArrays.TryRemove(completion.UserData, out var fixedArray))
                    {
                        fixedArray.Dispose();
                    }
                    tcs.SetResult(completion.Result);
                }
            }
        }
    }

    public Task<int> ReadAsync(FileDescriptor handle, byte[] buffer, int count, ulong offset)
    {
        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<object>();
        var fixedArray = new FixedArray<byte>(buffer);
        _pendingTasks.TryAdd(id, tcs);
        _fixedArrays.TryAdd(id, fixedArray);
        _ring.Issue(sub =>
        {
            unsafe
            {
                sub.UserData = id;
                sub.PrepareRead(handle, (void*)fixedArray.DangerousGetHandle(), count, offset);
            }
        });
        _ring.Submit();
        return tcs.Task.ContinueWith(t => (int)t.Result);
    }

    public Task<int> WriteAsync(FileDescriptor handle, byte[] buffer, int count, ulong offset)
    {
        var id = Interlocked.Increment(ref _nextId);
        var tcs = new TaskCompletionSource<object>();
        var fixedArray = new FixedArray<byte>(buffer);
        _pendingTasks.TryAdd(id, tcs);
        _fixedArrays.TryAdd(id, fixedArray);
        _ring.Issue(sub =>
        {
            unsafe
            {
                sub.UserData = id;
                sub.PrepareWrite(handle, (void*)fixedArray.DangerousGetHandle(), count, offset);
            }
        });
        _ring.Submit();
        return tcs.Task.ContinueWith(t => (int)t.Result);
    }

    public void Dispose()
    {
        _running = false;
        _ring.Dispose();
        foreach (var fixedArray in _fixedArrays.Values)
        {
            fixedArray.Dispose();
        }
        _completionThread.Join();
    }
}