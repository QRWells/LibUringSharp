using LibUringSharp.Enums;
using LibUringSharp.Exceptions;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp;

public sealed partial class Ring
{
    public void RegisterEventFd(FileDescriptor fd)
    {
        int ret;
        unsafe
        {
            int fileDescriptor = fd;
            ret = io_uring_register(_ringFd, IORING_REGISTER_EVENTFD, &fileDescriptor, 1);
        }

        if (ret < 0) throw new RegisterEventFdFailedException();
    }

    public void UnregisterEventFd()
    {
        int ret;
        unsafe
        {
            ret = io_uring_register(_ringFd, IORING_UNREGISTER_EVENTFD, null, 1);
        }

        if (ret < 0) throw new RegisterEventFdFailedException();
    }

    public void RegisterEventFdAsync(FileDescriptor fd)
    {
        int ret;
        unsafe
        {
            int fileDescriptor = fd;
            ret = io_uring_register(_ringFd, IORING_REGISTER_EVENTFD_ASYNC, &fileDescriptor, 1);
        }

        if (ret < 0) throw new RegisterEventFdFailedException();
    }



    private unsafe void RegisterRingFd()
    {
        var up = new io_uring_rsrc_update
        {
            offset = _enterRingFd
        };

        var ret = io_uring_register(_ringFd, IORING_UNREGISTER_RING_FDS, &up, 1);

        if (ret != 1) return;
        _enterRingFd = _ringFd;
        _intFlags &= ~RingInterrupt.RegRing;
    }

    internal unsafe int RegisterProbe(io_uring_probe* p, uint nrOps)
    {
        return io_uring_register(_ringFd, IORING_REGISTER_PROBE, p, nrOps);
    }
}