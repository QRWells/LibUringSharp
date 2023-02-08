using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Linux.LibC;

namespace LibUringSharp;

public sealed class RingProbe
{
    /// <summary>
    ///     last opcode supported
    /// </summary>
    private readonly byte _lastOp;

    private readonly bool[] _supportedOps;

    public RingProbe()
    {
        using var ring = new Ring(2);
        const uint len = io_uring_probe.Size + 256 * io_uring_probe_op.Size;
        unsafe
        {
            var probe = (io_uring_probe*)NativeMemory.Alloc((int)len);
            NativeMemory.Alloc(len);
            Unsafe.InitBlockUnaligned(probe, 0, len);
            var ret = ring.RegisterProbe(probe, 256);
            if (ret >= 0)
            {
                _lastOp = probe->last_op;
                var ops = io_uring_probe.ops(probe);
                var opsLen = probe->ops_len;
                _supportedOps = new bool[opsLen];
                for (var i = 0; i < opsLen; i++) _supportedOps[i] = (ops[i].flags & IO_URING_OP_SUPPORTED) != 0;
                NativeMemory.Free(probe);
                return;
            }

            NativeMemory.Free(probe);
            throw new Exception("io_uring_register_probe failed");
        }
    }

    public bool this[IoUringOp op] => HasOp(op);

    public bool HasOp(IoUringOp op)
    {
        return (byte)op <= _lastOp && _supportedOps[(byte)op];
    }
}