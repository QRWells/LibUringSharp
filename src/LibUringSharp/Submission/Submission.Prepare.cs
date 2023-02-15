using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using LibUringSharp.Linux.Handles;
using static LibUringSharp.Linux.LibC;

namespace LibUringSharp.Submission;

public readonly unsafe partial struct Submission
{
    #region I/O

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void PrepareReadWrite(IoUringOp op, int fd, void* addr, uint len, ulong offset)
    {
        unchecked
        {
            _sqe->opcode = (byte)op;
            _sqe->flags = 0;
            _sqe->ioprio = 0;
            _sqe->fd = fd;
            _sqe->off = offset;
            _sqe->addr = (nuint)addr;
            _sqe->len = len;
            _sqe->rw_flags = 0;
            _sqe->buf_index = 0;
            _sqe->personality = 0;
            _sqe->file_index = 0;
            _sqe->addr3 = 0;
            _sqe->__pad2 = 0;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReadV(FileDescriptor fd, IoVector* ioVectors, int length, ulong offset)
    {
        PrepareReadWrite(IoUringOp.ReadV, fd, ioVectors, (uint)length, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReadV(FileDescriptor fd, IoVector* ioVectors, int length, ulong offset, int flags)
    {
        PrepareReadV(fd, ioVectors, length, offset);
        _sqe->rw_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReadFixed(FileDescriptor fd, void* buf, int length, ulong offset, int bufIndex)
    {
        PrepareReadWrite(IoUringOp.ReadFixed, fd, buf, (uint)length, offset);
        _sqe->buf_index = (ushort)bufIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteV(FileDescriptor fd, IoVector* ioVectors, int length, ulong offset)
    {
        PrepareReadWrite(IoUringOp.WriteV, fd, ioVectors, (uint)length, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteV(FileDescriptor fd, IoVector* ioVectors, int length, ulong offset, int flags)
    {
        PrepareWriteV(fd, ioVectors, length, offset);
        _sqe->rw_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteFixed(FileDescriptor fd, void* buf, int length, ulong offset, int bufIndex)
    {
        PrepareReadWrite(IoUringOp.WriteFixed, fd, buf, (uint)length, offset);
        _sqe->buf_index = (ushort)bufIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTargetFixedFile(uint fileIndex)
    {
        // 0 means no fixed files, indexes should be encoded as "index + 1"
        _sqe->file_index = fileIndex + 1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareOpenAt(int dfd, char* path, int flags, uint mode)
    {
        PrepareReadWrite(IoUringOp.OpenAt, dfd, path, mode, 0);
        _sqe->open_flags = (uint)flags;
    }

    /// <summary>
    ///     open directly into the fixed file table
    /// </summary>
    /// <param name="dfd"></param>
    /// <param name="path"></param>
    /// <param name="flags"></param>
    /// <param name="mode"></param>
    /// <param name="fileIndex"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareOpenAtDirect(int dfd, char* path, int flags, uint mode, uint fileIndex)
    {
        PrepareOpenAt(dfd, path, flags, mode);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareOpenAt2(int dfd, char* path, open_how* how)
    {
        PrepareReadWrite(IoUringOp.OpenAt2, dfd, path, open_how.Size, (ulong)how);
    }

    /* open directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareOpenAt2Direct(int dfd, char* path, open_how* how, uint fileIndex)
    {
        PrepareOpenAt2(dfd, path, how);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareEpollCtl(int epFd, int fd, int op, epoll_event* ev)
    {
        PrepareReadWrite(IoUringOp.EpollCtl, epFd, ev, (uint)op, (uint)fd);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareClose(FileDescriptor fd)
    {
        PrepareReadWrite(IoUringOp.Close, fd, null, 0, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCloseDirect(uint fileIndex)
    {
        PrepareClose(0);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRead(FileDescriptor fd, void* buf, int length, ulong offset)
    {
        PrepareReadWrite(IoUringOp.Read, fd, buf, (uint)length, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSelectRead(FileDescriptor fd, ushort bufGroup, uint len, ulong offset)
    {
        PrepareReadWrite(IoUringOp.Read, fd, null, len, offset);
        _sqe->flags |= (byte)SubmissionOption.BufferSelect;
        _sqe->buf_group = bufGroup;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWrite(FileDescriptor fd, void* buf, int length, ulong offset)
    {
        PrepareReadWrite(IoUringOp.Write, fd, buf, (uint)length, offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSyncFileRange(int fd, uint len, ulong offset, int flags)
    {
        PrepareReadWrite(IoUringOp.SyncFileRange, fd, null, len, offset);
        _sqe->sync_range_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCancel64(nuint userData, int flags)
    {
        PrepareReadWrite(IoUringOp.AsyncCancel, -1, null, 0, 0);
        _sqe->addr = userData;
        _sqe->cancel_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCancel(void* userData, int flags)
    {
        PrepareCancel64((nuint)userData, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCancelFd(int fd, uint flags)
    {
        PrepareReadWrite(IoUringOp.AsyncCancel, fd, null, 0, 0);
        _sqe->cancel_flags = flags | IORING_ASYNC_CANCEL_FD;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFAllocate(FileDescriptor fd, int mode, long offset, long len)
    {
        PrepareReadWrite(IoUringOp.FAllocate, fd, null, (uint)mode, (ulong)offset);
        _sqe->addr = (nuint)len;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFilesUpdate(int* fds, int num, int offset)
    {
        PrepareReadWrite(IoUringOp.FilesUpdate, -1, fds, (uint)num, (ulong)offset);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareStatX(int dfd, char* path, int flags, uint mask, StatX* statX)
    {
        PrepareReadWrite(IoUringOp.StatX, dfd, path, mask, (ulong)statX);
        _sqe->statx_flags = (uint)flags;
    }

    #endregion

    #region Send/Receive

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessage(FileDescriptor fd, MsgHeader* msg, uint flags)
    {

        PrepareReadWrite(IoUringOp.ReceiveMsg, fd, msg, 1, 0);
        _sqe->msg_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessageMultiShot(FileDescriptor fd, MsgHeader* msg, uint flags)
    {
        PrepareReceiveMessage(fd, msg, flags);
        _sqe->ioprio |= IORING_RECV_MULTISHOT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendMessage(FileDescriptor fd, MsgHeader* msg, uint flags)
    {
        PrepareReadWrite(IoUringOp.SendMsg, fd, msg, 1, 0);
        _sqe->msg_flags = flags;
    }

    #endregion

    #region Poll

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint PreparePollMask(uint pollMask)
    {
        return BitConverter.IsLittleEndian ? pollMask : BinaryPrimitives.ReverseEndianness(pollMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollAdd(FileDescriptor fd, uint pollMask)
    {
        PrepareReadWrite(IoUringOp.PollAdd, fd, null, 0, 0);
        _sqe->poll32_events = PreparePollMask(pollMask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollMultiShot(FileDescriptor fd, uint pollMask)
    {
        PreparePollAdd(fd, pollMask);
        _sqe->len = IORING_POLL_ADD_MULTI;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollRemove(ulong userData)
    {
        PrepareReadWrite(IoUringOp.PollRemove, -1, null, 0, 0);
        _sqe->addr = userData;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollUpdate(ulong oldUserData, ulong newUserData, uint pollMask, uint flags)
    {
        PrepareReadWrite(IoUringOp.PollRemove, -1, null, flags,
            newUserData);
        _sqe->addr = oldUserData;
        _sqe->poll32_events = PreparePollMask(pollMask);
    }

    #endregion

    #region Timeout

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTimeout(__kernel_timespec* ts, uint count, uint flags)
    {
        PrepareReadWrite(IoUringOp.Timeout, -1, ts, 1, count);
        _sqe->timeout_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTimeoutRemove(ulong userData, uint flags)
    {
        PrepareReadWrite(IoUringOp.TimeoutRemove, -1, null, 0, 0);
        _sqe->addr = userData;
        _sqe->timeout_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTimeoutUpdate(__kernel_timespec* ts, ulong userData, uint flags)
    {
        PrepareReadWrite(IoUringOp.TimeoutRemove, -1, null, 0, (ulong)ts);
        _sqe->addr = userData;
        _sqe->timeout_flags = flags | IORING_TIMEOUT_UPDATE;
    }

    #endregion

    #region Network

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAccept(int fd, SocketAddr* addr, uint* addrLen, int flags)
    {
        PrepareReadWrite(IoUringOp.Accept, fd, addr, 0, (ulong)addrLen);
        _sqe->accept_flags = (uint)flags;
    }

    /* accept directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAcceptDirect(int fd, SocketAddr* addr, uint* addrLen, int flags, uint fileIndex)
    {
        PrepareAccept(fd, addr, addrLen, flags);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultiShotAccept(int fd, SocketAddr* addr, uint* addrLen, int flags)
    {
        PrepareAccept(fd, addr, addrLen, flags);
        _sqe->ioprio |= (ushort)IORING_ACCEPT_MULTISHOT;
    }

    /* multi shot accept directly into the fixed file table */
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
    public void PrepareLinkTimeout(__kernel_timespec* ts, uint flags)
    {
        PrepareReadWrite(IoUringOp.LinkTimeout, -1, ts, 1, 0);
        _sqe->timeout_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSend(int socketFd, void* buf, ulong len, int flags)
    {
        PrepareReadWrite(IoUringOp.Send, socketFd, buf, (uint)len, 0);
        _sqe->msg_flags = (uint)flags;
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
    public void io_uring_prep_socket(SocketDomain domain, SocketType type, SocketProtocol protocol, uint flags)
    {
        PrepareReadWrite(IoUringOp.Socket, (int)domain, null, (uint)protocol, (ulong)type);
        _sqe->rw_flags = (int)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void io_uring_prep_socket_direct(SocketDomain domain, SocketType type, SocketProtocol protocol,
        uint fileIndex, uint flags)
    {
        PrepareReadWrite(IoUringOp.Socket, (int)domain, null, (uint)protocol, (ulong)type);
        _sqe->rw_flags = (int)flags;
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void io_uring_prep_socket_direct_alloc(SocketDomain domain, SocketType type, SocketProtocol protocol,
        uint flags)
    {
        PrepareReadWrite(IoUringOp.Socket, (int)domain, null, (uint)protocol, (ulong)type);
        _sqe->rw_flags = (int)flags;
        SetTargetFixedFile(IORING_FILE_INDEX_ALLOC - 1);
    }

    #endregion

    #region Buffer

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareProvideBuffers(void* addr, int len, int nr, int bufGroupId, int bufId)
    {
        PrepareReadWrite(IoUringOp.ProvideBuffers, nr, addr, (uint)len, (ulong)bufId);
        _sqe->buf_group = (ushort)bufGroupId;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRemoveBuffers(int nr, int bufGroupId)
    {
        PrepareReadWrite(IoUringOp.RemoveBuffers, nr, null, 0, 0);
        _sqe->buf_group = (ushort)bufGroupId;
    }

    #endregion

    #region Misc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFSync(FileDescriptor fd, uint fsyncFlags)
    {
        PrepareReadWrite(IoUringOp.FSync, fd, null, 0, 0);
        _sqe->fsync_flags = fsyncFlags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareNop(ulong userData = 0, SubmissionOption options = SubmissionOption.None, ushort personality = 0)
    {
        unchecked
        {
            _sqe->opcode = (byte)IoUringOp.Nop;
            _sqe->flags = (byte)options;
            _sqe->fd = -1;
            _sqe->user_data = userData;
            _sqe->personality = personality;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFAdvise(int fd, ulong offset, long len, int advice)
    {
        PrepareReadWrite(IoUringOp.FAdvise, fd, null, (uint)len, offset);
        _sqe->fadvise_advice = (uint)advice;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMAdvise(void* addr, long length, int advice)
    {
        PrepareReadWrite(IoUringOp.MAdvise, -1, addr, (uint)length, 0);
        _sqe->fadvise_advice = (uint)advice;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSplice(int fdIn, ulong offIn, int fdOut, ulong offOut, uint bytes, uint spliceFlags)
    {
        PrepareReadWrite(IoUringOp.Splice, fdOut, null, bytes, offOut);
        _sqe->splice_off_in = offIn;
        _sqe->splice_fd_in = fdIn;
        _sqe->splice_flags = spliceFlags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTee(int fdIn, int fdOut, uint nBytes, uint spliceFlags)
    {
        PrepareReadWrite(IoUringOp.Tee, fdOut, null, nBytes, 0);
        _sqe->splice_off_in = 0;
        _sqe->splice_fd_in = fdIn;
        _sqe->splice_flags = spliceFlags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareShutdown(int fd, int how)
    {
        PrepareReadWrite(IoUringOp.Shutdown, fd, null, (uint)how, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRenameAt(int oldFd, char* oldPath, int newFd, char* newPath, uint flags)
    {
        PrepareReadWrite(IoUringOp.RenameAt, oldFd, oldPath, (uint)newFd, (nuint)newPath);
        _sqe->rename_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRename(char* oldPath, char* newPath, uint flags)
    {
        PrepareRenameAt((int)AtFile.FdCurrentWorkingDirectory, oldPath,
            (int)AtFile.FdCurrentWorkingDirectory, newPath, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareLinkAt(int oldFd, char* oldPath, int newFd, char* newPath, uint flags)
    {
        PrepareReadWrite(IoUringOp.LinkAt, oldFd, oldPath, (uint)newFd, (nuint)newPath);

        _sqe->hardlink_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareLink(char* oldPath, char* newPath, uint flags)
    {
        PrepareLinkAt((int)AtFile.FdCurrentWorkingDirectory, oldPath,
            (int)AtFile.FdCurrentWorkingDirectory, newPath, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareUnlinkAt(int fd, char* path, uint flags)
    {
        PrepareReadWrite(IoUringOp.UnlinkAt, fd, path, 0, 0);

        _sqe->unlink_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareUnlink(char* path, uint flags)
    {
        PrepareUnlinkAt((int)AtFile.FdCurrentWorkingDirectory, path, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSymlinkAt(char* target, int newFd, char* linkPath)
    {
        PrepareReadWrite(IoUringOp.SymlinkAt, newFd, target, 0, (nuint)linkPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSymlink(char* target, char* linkPath)
    {
        PrepareSymlinkAt(target, (int)AtFile.FdCurrentWorkingDirectory, linkPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMakeDirAt(int fd, char* path, uint mode)
    {
        PrepareReadWrite(IoUringOp.MkdirAt, fd, path, mode, 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMakeDir(char* path, uint mode)
    {
        PrepareMakeDirAt((int)AtFile.FdCurrentWorkingDirectory, path, mode);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMessageRingCompletionFlags(int fd, uint len, ulong data, uint flags, uint cqeFlags)
    {
        PrepareReadWrite(IoUringOp.MsgRing, fd, null, len, data);
        _sqe->msg_ring_flags = IORING_MSG_RING_FLAGS_PASS | flags;
        _sqe->file_index = cqeFlags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMessageRing(int fd, uint len, ulong data, uint flags)
    {
        PrepareReadWrite(IoUringOp.MsgRing, fd, null, len, data);
        _sqe->msg_ring_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepGetXAttr(char* name, char* path, void* value, int length)
    {
        PrepareReadWrite(IoUringOp.GetXAttr, 0, name, (uint)length, (nuint)value);
        _sqe->addr3 = (nuint)path;

        _sqe->xattr_flags = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSetXAttr(char* name, char* path, void* value, int length, int flags)
    {
        PrepareReadWrite(IoUringOp.SetXAttr, 0, name, (uint)length, (nuint)value);
        _sqe->addr3 = (nuint)path;

        _sqe->xattr_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepFGetXAttr(int fd, char* name, void* value, int length)
    {
        PrepareReadWrite(IoUringOp.FGetXAttr, fd, name, (uint)length, (nuint)value);

        _sqe->xattr_flags = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFSetXAttr(int fd, char* name, void* value, int length, int flags)
    {
        PrepareReadWrite(IoUringOp.FSetXAttr, fd, name, (uint)length, (nuint)value);
        _sqe->xattr_flags = (uint)flags;
    }

    #endregion
}