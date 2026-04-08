using System.Runtime.CompilerServices;
using QRWells.LibUringSharp.Linux.Handles;
using static QRWells.LibUringSharp.Linux.LibC;

namespace QRWells.LibUringSharp.Submission;

public readonly unsafe partial struct Submission
{
    #region Network

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAccept(int fd, SocketAddr* addr, uint* addrLen, int flags)
    {
        PrepareReadWrite(IoUringOp.Accept, fd, addr, 0, (ulong)addrLen);
        _sqe->accept_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAcceptDirect(int fd, SocketAddr* addr, uint* addrLen, int flags, uint fileIndex)
    {
        PrepareAccept(fd, addr, addrLen, flags);
        if (fileIndex == IORING_FILE_INDEX_ALLOC) fileIndex--;
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultiShotAccept(int fd, SocketAddr* addr, uint* addrLen, int flags)
    {
        PrepareAccept(fd, addr, addrLen, flags);
        _sqe->ioprio |= (ushort)IORING_ACCEPT_MULTISHOT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultiShotAcceptDirect(int fd, SocketAddr* addr, uint* addrLen, int flags)
    {
        PrepareMultiShotAccept(fd, addr, addrLen, flags);
        SetTargetFixedFile(IORING_FILE_INDEX_ALLOC - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareConnect(int fd, SocketAddr* addr, uint addrLen)
    {
        PrepareReadWrite(IoUringOp.Connect, fd, addr, 0, addrLen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareBind(int fd, SocketAddr* addr, uint addrLen)
    {
        PrepareReadWrite(IoUringOp.Bind, fd, addr, 0, addrLen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareListen(int fd, int backlog)
    {
        PrepareReadWrite(IoUringOp.Listen, fd, null, (uint)backlog, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSend(int socketFd, void* buf, ulong len, int flags)
    {
        PrepareReadWrite(IoUringOp.Send, socketFd, buf, (uint)len, 0);
        _sqe->msg_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendTo(FileDescriptor socket_fd, void* buf, ulong len, int flags, SocketAddr* addr,
        ushort addrLen)
    {
        PrepareSend(socket_fd, buf, len, flags);
        PrepareSendSetAddr(addr, addrLen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendZc(int socketFd, void* buf, ulong len, int flags, uint zcFlags)
    {
        PrepareReadWrite(IoUringOp.SendZc, socketFd, buf, (uint)len, 0);
        _sqe->msg_flags = (uint)flags;
        _sqe->ioprio = (ushort)zcFlags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendZcFixed(int socketFd, void* buf, ulong len, int flags, uint zcFlags, uint bufIndex)
    {
        PrepareSendZc(socketFd, buf, len, flags, zcFlags);
        _sqe->ioprio |= IORING_RECVSEND_FIXED_BUF;
        _sqe->buf_index = (ushort)bufIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendMessageZc(int fd, MsgHeader* msg, uint flags)
    {
        PrepareSendMessage(fd, msg, flags);
        _sqe->opcode = (byte)IoUringOp.SendMsgZc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendSetAddr(SocketAddr* destAddr, ushort addrLen)
    {
        _sqe->addr2 = (ulong)destAddr;
        _sqe->addr_len = addrLen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceive(int socketFd, void* buf, ulong len, int flags)
    {
        PrepareReadWrite(IoUringOp.Receive, socketFd, buf, (uint)len, 0);
        _sqe->msg_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMultiShot(int socketFd, void* buf, ulong len, int flags)
    {
        PrepareReceive(socketFd, buf, len, flags);
        _sqe->ioprio |= IORING_RECV_MULTISHOT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSocket(SocketDomain domain, SocketType type, SocketProtocol protocol, uint flags)
    {
        PrepareReadWrite(IoUringOp.Socket, (int)domain, null, (uint)protocol, (ulong)type);
        _sqe->rw_flags = (int)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSocketDirect(SocketDomain domain, SocketType type, SocketProtocol protocol, uint fileIndex,
        uint flags)
    {
        PrepareSocket(domain, type, protocol, flags);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSocketDirectAllocate(SocketDomain domain, SocketType type, SocketProtocol protocol, uint flags)
    {
        PrepareSocket(domain, type, protocol, flags);
        SetTargetFixedFile(IORING_FILE_INDEX_ALLOC - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void io_uring_prep_socket(SocketDomain domain, SocketType type, SocketProtocol protocol, uint flags)
    {
        PrepareSocket(domain, type, protocol, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void io_uring_prep_socket_direct(SocketDomain domain, SocketType type, SocketProtocol protocol,
        uint fileIndex, uint flags)
    {
        PrepareSocketDirect(domain, type, protocol, fileIndex, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void io_uring_prep_socket_direct_alloc(SocketDomain domain, SocketType type, SocketProtocol protocol,
        uint flags)
    {
        PrepareSocketDirectAllocate(domain, type, protocol, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareShutdown(int fd, int how)
    {
        PrepareReadWrite(IoUringOp.Shutdown, fd, null, (uint)how, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePipe(int* fds, int pipeFlags)
    {
        PrepareReadWrite(IoUringOp.Pipe, 0, fds, 0, 0);
        _sqe->pipe_flags = (uint)pipeFlags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePipeDirect(int* fds, int pipeFlags, uint fileIndex)
    {
        PreparePipe(fds, pipeFlags);
        if (fileIndex == IORING_FILE_INDEX_ALLOC) fileIndex--;
        SetTargetFixedFile(fileIndex);
    }

    #endregion
}
