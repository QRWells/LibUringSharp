using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Linux.LibC;

namespace LibUringSharp;

public class RingProbe : IDisposable
{
    private readonly unsafe io_uring_probe* _probePtr;

    public RingProbe()
    {
        using var ring = new Ring(2);
        const uint len = io_uring_probe.Size + 256 * io_uring_probe_op.Size;
        unsafe
        {
            var probe = (io_uring_probe*)Marshal.AllocHGlobal((int)len);
            Unsafe.InitBlock(probe, 0, len);
            var ret = ring.RegisterProbe(probe, 256);
            if (ret >= 0)
            {
                _probePtr = probe;
                return;
            }

            Marshal.FreeHGlobal((nint)probe);
            throw new Exception("io_uring_register_probe failed");
        }
    }

    public void Dispose()
    {
        unsafe
        {
            Marshal.FreeHGlobal((nint)_probePtr);
        }

        GC.SuppressFinalize(this);
    }

    public bool HasOp(IoUringOp op)
    {
        unsafe
        {
            if ((byte)op > _probePtr->last_op) return false;
            var ops = io_uring_probe.ops(_probePtr);
            return (ops[(byte)op].flags & IO_URING_OP_SUPPORTED) != 0;
        }
    }
}