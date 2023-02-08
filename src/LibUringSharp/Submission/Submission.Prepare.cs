using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Linux.Handles;
using static Linux.LibC;

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
    public void PrepareReadV(FileDescriptor fd, Span<iovec> ioVectors, ulong offset)
    {
        fixed (iovec* ioVectorsPtr = ioVectors)
        {
            PrepareReadWrite(IoUringOp.ReadV, fd, ioVectorsPtr, (uint)ioVectors.Length, offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReadV(FileDescriptor fd, Span<iovec> ioVectors, ulong offset, int flags)
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
    public void PrepareWriteV(FileDescriptor fd, Span<iovec> ioVectors, ulong offset)
    {
        fixed (iovec* ioVectorsPtr = ioVectors)
        {
            PrepareReadWrite(IoUringOp.WriteV, fd, ioVectorsPtr, (uint)ioVectors.Length, offset);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteV(FileDescriptor fd, Span<iovec> ioVectors, ulong offset, int flags)
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
    public void PrepareStatX(int dfd, string path, int flags, uint mask, ref statx statx)
    {
        fixed (char* pathPtr = path)
        {
            fixed (statx* statxPtr = &statx)
            {
                PrepareReadWrite(IoUringOp.StatX, dfd, pathPtr, mask, (ulong)statxPtr);
                _sqe->statx_flags = (uint)flags;
            }
        }
    }

    #endregion

    #region Send/Receive

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessage(FileDescriptor fd, ref msghdr msg, uint flags)
    {
        fixed (msghdr* msgPtr = &msg)
        {
            PrepareReadWrite(IoUringOp.ReceiveMsg, fd, msgPtr, 1, 0);
            _sqe->msg_flags = flags;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessageMultiShot(FileDescriptor fd, ref msghdr msg, uint flags)
    {
        PrepareReceiveMessage(fd, ref msg, flags);
        _sqe->ioprio |= IORING_RECV_MULTISHOT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendMessage(FileDescriptor fd, ref msghdr msg, uint flags)
    {
        fixed (msghdr* msgPtr = &msg)
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
    public void PrepareAccept(int fd, ref sockaddr addr, ref uint addrLen, int flags)
    {
        fixed (sockaddr* addrPtr = &addr)
        {
            PrepareReadWrite(IoUringOp.Accept, fd, addrPtr, 0, addrLen);
            _sqe->accept_flags = (uint)flags;
        }
    }

    /* accept directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAcceptDirect(int fd, ref sockaddr addr, ref uint addrLen, int flags, uint fileIndex)
    {
        PrepareAccept(fd, ref addr, ref addrLen, flags);
        SetTargetFixedFile(fileIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultiShotAccept(int fd, ref sockaddr addr, ref uint addrLen, int flags)
    {
        PrepareAccept(fd, ref addr, ref addrLen, flags);
        _sqe->ioprio |= (ushort)IORING_ACCEPT_MULTISHOT;
    }

    /* multi shot accept directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultiShotAcceptDirect(int fd, ref sockaddr addr, ref uint addrLen, int flags)
    {
        PrepareMultiShotAccept(fd, ref addr, ref addrLen, flags);
        SetTargetFixedFile(IORING_FILE_INDEX_ALLOC - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareConnect(int fd, ref sockaddr addr, uint addrLen)
    {
        fixed (sockaddr* addrPtr = &addr)
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
    public void PrepareSendMessageZc(int fd, ref msghdr msg, uint flags)
    {
        PrepareSendMessage(fd, ref msg, flags);
        _sqe->opcode = (byte)IoUringOp.SendMsgZc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendSetAddr(ref sockaddr destAddr, ushort addrLen)
    {
        fixed (sockaddr* destAddrPtr = &destAddr)
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

    #endregion
}