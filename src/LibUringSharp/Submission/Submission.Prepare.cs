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
            _sqe->addr = (ulong)addr;
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
    public void PrepareReadV(FileDescriptor fd, Span<IoVector> ioVectors, ulong offset)
    {
        fixed (IoVector* ioVectorsPtr = ioVectors)
        {
            PrepareReadWrite(IoUringOp.ReadV, fd, ioVectorsPtr, (uint)ioVectors.Length, offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReadV(FileDescriptor fd, Span<IoVector> ioVectors, ulong offset, int flags)
    {
        PrepareReadV(fd, ioVectors, offset);
        _sqe->rw_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReadFixed(FileDescriptor fd, Span<byte> buf, ulong offset, int bufIndex)
    {
        fixed (void* bufPtr = buf)
        {
            PrepareReadWrite(IoUringOp.ReadFixed, fd, bufPtr, (uint)buf.Length, offset);
            _sqe->buf_index = (ushort)bufIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteV(FileDescriptor fd, Span<IoVector> ioVectors, ulong offset)
    {
        fixed (IoVector* ioVectorsPtr = ioVectors)
        {
            PrepareReadWrite(IoUringOp.WriteV, fd, ioVectorsPtr, (uint)ioVectors.Length, offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteV(FileDescriptor fd, Span<IoVector> ioVectors, ulong offset, int flags)
    {
        PrepareWriteV(fd, ioVectors, offset);
        _sqe->rw_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteFixed(FileDescriptor fd, Span<byte> buf, ulong offset, int bufIndex)
    {
        fixed (void* bufPtr = buf)
        {
            PrepareReadWrite(IoUringOp.WriteFixed, fd, bufPtr, (uint)buf.Length, offset);
            _sqe->buf_index = (ushort)bufIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SetTargetFixedFile(uint fileIndex)
    {
        // 0 means no fixed files, indexes should be encoded as "index + 1"
        _sqe->file_index = fileIndex + 1;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareOpenAt(int dfd, string path, int flags, uint mode)
    {
        fixed (char* pathPtr = path)
        {
            PrepareReadWrite(IoUringOp.OpenAt, dfd, pathPtr, mode, 0);
            _sqe->open_flags = (uint)flags;
        }
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
    public void PrepareOpenAtDirect(int dfd, string path, int flags, uint mode, uint fileIndex)
    {
        PrepareOpenAt(dfd, path, flags, mode);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareOpenAt2(int dfd, string path, ref open_how how)
    {
        fixed (char* pathPtr = path)
        {
            fixed (open_how* howPtr = &how)
            {
                PrepareReadWrite(IoUringOp.OpenAt2, dfd, pathPtr, open_how.Size, (ulong)howPtr);
            }
        }
    }

    /* open directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareOpenAt2Direct(int dfd, string path, ref open_how how, uint fileIndex)
    {
        PrepareOpenAt2(dfd, path, ref how);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareEpollCtl(int epFd, int fd, int op, ref epoll_event ev)
    {
        fixed (epoll_event* evPtr = &ev)
        {
            PrepareReadWrite(IoUringOp.EpollCtl, epFd, evPtr, (uint)op, (uint)fd);
        }
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
    public void PrepareRead(FileDescriptor fd, Span<byte> buf, ulong offset)
    {
        fixed (void* bufPtr = buf)
        {
            PrepareReadWrite(IoUringOp.Read, fd, bufPtr, (uint)buf.Length, offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSelectRead(FileDescriptor fd, ushort bufGroup, uint len, ulong offset)
    {
        PrepareReadWrite(IoUringOp.Read, fd, null, len, offset);
        _sqe->flags |= (byte)SubmissionOption.BufferSelect;
        _sqe->buf_group = bufGroup;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWrite(FileDescriptor fd, Span<byte> buf, ulong offset)
    {
        fixed (void* bufPtr = buf)
        {
            PrepareReadWrite(IoUringOp.Write, fd, bufPtr, (uint)buf.Length, offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSyncFileRange(int fd, uint len, ulong offset, int flags)
    {
        PrepareReadWrite(IoUringOp.SyncFileRange, fd, null, len, offset);
        _sqe->sync_range_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCancel64(ulong userData, int flags)
    {
        PrepareReadWrite(IoUringOp.AsyncCancel, -1, null, 0, 0);
        _sqe->addr = userData;
        _sqe->cancel_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareCancel(void* userData, int flags)
    {
        PrepareCancel64((ulong)userData, flags);
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
        _sqe->addr = (ulong)len;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFilesUpdate(Span<FileDescriptor> fds, int offset)
    {
        fixed (int* fdsPtr = fds.ToIntSpan())
        {
            PrepareReadWrite(IoUringOp.FilesUpdate, -1, fdsPtr, (uint)fds.Length, (ulong)offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareStatX(int dfd, string path, int flags, uint mask, ref StatX statX)
    {
        fixed (char* pathPtr = path)
        {
            fixed (StatX* statXPtr = &statX)
            {
                PrepareReadWrite(IoUringOp.StatX, dfd, pathPtr, mask, (ulong)statXPtr);
                _sqe->statx_flags = (uint)flags;
            }
        }
    }

    #endregion

    #region Send/Receive

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessage(FileDescriptor fd, ref MsgHeader msg, uint flags)
    {
        fixed (MsgHeader* msgPtr = &msg)
        {
            PrepareReadWrite(IoUringOp.ReceiveMsg, fd, msgPtr, 1, 0);
            _sqe->msg_flags = flags;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessageMultiShot(FileDescriptor fd, ref MsgHeader msg, uint flags)
    {
        PrepareReceiveMessage(fd, ref msg, flags);
        _sqe->ioprio |= IORING_RECV_MULTISHOT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendMessage(FileDescriptor fd, ref MsgHeader msg, uint flags)
    {
        fixed (MsgHeader* msgPtr = &msg)
        {
            PrepareReadWrite(IoUringOp.SendMsg, fd, msgPtr, 1, 0);
            _sqe->msg_flags = flags;
        }
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
    public void PrepareTimeout(ref __kernel_timespec ts, uint count, uint flags)
    {
        fixed (__kernel_timespec* tsPtr = &ts)
        {
            PrepareReadWrite(IoUringOp.Timeout, -1, tsPtr, 1, count);
            _sqe->timeout_flags = flags;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTimeoutRemove(ulong userData, uint flags)
    {
        PrepareReadWrite(IoUringOp.TimeoutRemove, -1, null, 0, 0);
        _sqe->addr = userData;
        _sqe->timeout_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTimeoutUpdate(ref __kernel_timespec ts, ulong userData, uint flags)
    {
        fixed (__kernel_timespec* tsPtr = &ts)
        {
            PrepareReadWrite(IoUringOp.TimeoutRemove, -1, null, 0, (ulong)tsPtr);
            _sqe->addr = userData;
            _sqe->timeout_flags = flags | IORING_TIMEOUT_UPDATE;
        }
    }

    #endregion

    #region Network

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAccept(int fd, ref SocketAddr addr, ref uint addrLen, int flags)
    {
        fixed (SocketAddr* addrPtr = &addr)
        {
            PrepareReadWrite(IoUringOp.Accept, fd, addrPtr, 0, addrLen);
            _sqe->accept_flags = (uint)flags;
        }
    }

    /* accept directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAcceptDirect(int fd, ref SocketAddr addr, ref uint addrLen, int flags, uint fileIndex)
    {
        PrepareAccept(fd, ref addr, ref addrLen, flags);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultiShotAccept(int fd, ref SocketAddr addr, ref uint addrLen, int flags)
    {
        PrepareAccept(fd, ref addr, ref addrLen, flags);
        _sqe->ioprio |= (ushort)IORING_ACCEPT_MULTISHOT;
    }

    /* multi shot accept directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultiShotAcceptDirect(int fd, ref SocketAddr addr, ref uint addrLen, int flags)
    {
        PrepareMultiShotAccept(fd, ref addr, ref addrLen, flags);
        SetTargetFixedFile(IORING_FILE_INDEX_ALLOC - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareConnect(int fd, ref SocketAddr addr, uint addrLen)
    {
        fixed (SocketAddr* addrPtr = &addr)
        {
            PrepareReadWrite(IoUringOp.Connect, fd, addrPtr, 0, addrLen);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareLinkTimeout(ref __kernel_timespec ts, uint flags)
    {
        fixed (__kernel_timespec* tsPtr = &ts)
        {
            PrepareReadWrite(IoUringOp.LinkTimeout, -1, tsPtr, 1, 0);
            _sqe->timeout_flags = flags;
        }
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
    public void PrepareSendMessageZc(int fd, ref MsgHeader msg, uint flags)
    {
        PrepareSendMessage(fd, ref msg, flags);
        _sqe->opcode = (byte)IoUringOp.SendMsgZc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendSetAddr(ref SocketAddr destAddr, ushort addrLen)
    {
        fixed (SocketAddr* destAddrPtr = &destAddr)
        {
            _sqe->addr2 = (ulong)destAddrPtr;
            _sqe->addr_len = addrLen;
        }
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
    public void PrepareRenameAt(int oldFd, string oldPath, int newFd, string newPath, uint flags)
    {
        fixed (char* oldPathPtr = oldPath, newPathPtr = newPath)
        {
            PrepareReadWrite(IoUringOp.RenameAt, oldFd, oldPathPtr, (uint)newFd, (nuint)newPathPtr);
        }

        _sqe->rename_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRename(string oldPath, string newPath, uint flags)
    {
        PrepareRenameAt((int)AtFile.FdCurrentWorkingDirectory, oldPath,
            (int)AtFile.FdCurrentWorkingDirectory, newPath, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareLinkAt(int oldFd, string oldPath, int newFd, string newPath, uint flags)
    {
        fixed (char* oldPathPtr = oldPath, newPathPtr = newPath)
        {
            PrepareReadWrite(IoUringOp.LinkAt, oldFd, oldPathPtr, (uint)newFd, (nuint)newPathPtr);
        }

        _sqe->hardlink_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareLink(string oldPath, string newPath, uint flags)
    {
        PrepareLinkAt((int)AtFile.FdCurrentWorkingDirectory, oldPath,
            (int)AtFile.FdCurrentWorkingDirectory, newPath, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareUnlinkAt(int fd, string path, uint flags)
    {
        fixed (char* pathPtr = path)
        {
            PrepareReadWrite(IoUringOp.UnlinkAt, fd, pathPtr, 0, 0);
        }

        _sqe->unlink_flags = flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareUnlink(string path, uint flags)
    {
        PrepareUnlinkAt((int)AtFile.FdCurrentWorkingDirectory, path, flags);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSymlinkAt(string target, int newFd, string linkPath)
    {
        fixed (char* targetPtr = target, linkPathPtr = linkPath)
        {
            PrepareReadWrite(IoUringOp.SymlinkAt, newFd, targetPtr, 0, (nuint)linkPathPtr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSymlink(string target, string linkPath)
    {
        PrepareSymlinkAt(target, (int)AtFile.FdCurrentWorkingDirectory, linkPath);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMakeDirAt(int fd, string path, uint mode)
    {
        fixed (char* pathPtr = path)
        {
            PrepareReadWrite(IoUringOp.MkdirAt, fd, pathPtr, mode, 0);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMakeDir(string path, uint mode)
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
    public void PrepGetXAttr(string name, string path, Span<byte> value)
    {
        fixed (char* namePtr = name, pathPtr = path)
        {
            fixed (byte* valuePtr = value)
            {
                PrepareReadWrite(IoUringOp.GetXAttr, 0, namePtr, (uint)value.Length, (nuint)valuePtr);
                _sqe->addr3 = (nuint)pathPtr;
            }
        }

        _sqe->xattr_flags = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSetXAttr(string name, string path, Span<byte> value, int flags)
    {
        fixed (char* namePtr = name, pathPtr = path)
        {
            fixed (byte* valuePtr = value)
            {
                PrepareReadWrite(IoUringOp.SetXAttr, 0, namePtr, (uint)value.Length, (nuint)valuePtr);
                _sqe->addr3 = (nuint)pathPtr;
            }
        }

        _sqe->xattr_flags = (uint)flags;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepFGetXAttr(
        int fd, string name,
        Span<byte> value)
    {
        fixed (char* namePtr = name)
        {
            fixed (byte* valuePtr = value)
            {
                PrepareReadWrite(IoUringOp.FGetXAttr, fd, namePtr, (uint)value.Length, (nuint)valuePtr);
            }
        }

        _sqe->xattr_flags = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFSetXAttr(int fd, string name, Span<byte> value, int flags)
    {
        fixed (char* namePtr = name)
        {
            fixed (byte* valuePtr = value)
            {
                PrepareReadWrite(IoUringOp.FSetXAttr, fd, namePtr, (uint)value.Length, (nuint)valuePtr);
            }
        }

        _sqe->xattr_flags = (uint)flags;
    }

    #endregion
}