using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using LibUringSharp.Enums;
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
            _queue.NotifyPrepared(_index);
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
            _queue.NotifyPrepared(_index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWriteV(FileDescriptor fd, Span<iovec> ioVectors, ulong offset)
    {
        fixed (iovec* ioVectorsPtr = ioVectors)
        {
            PrepareReadWrite(IoUringOp.WriteV, fd, ioVectorsPtr, (uint)ioVectors.Length, offset);
            _queue.NotifyPrepared(_index);
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
            _queue.NotifyPrepared(_index);
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
            _queue.NotifyPrepared(_index);
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
        _queue.NotifyPrepared(_index);
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
            _queue.NotifyPrepared(_index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareWrite(FileDescriptor fd, Span<byte> buf, ulong offset)
    {
        fixed (void* bufPtr = buf)
        {
            PrepareReadWrite(IoUringOp.Write, fd, bufPtr, (uint)buf.Length, offset);
            _queue.NotifyPrepared(_index);
        }
    }

    #endregion

    #region Send/Receive

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessage(FileDescriptor fd, msghdr* msg, uint flags)
    {
        PrepareReadWrite(IoUringOp.ReceiveMsg, fd, msg, 1, 0);
        _sqe->msg_flags = flags;
        _queue.NotifyPrepared(_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareReceiveMessageMultiShot(FileDescriptor fd, msghdr* msg, uint flags)
    {
        PrepareReceiveMessage(fd, msg, flags);
        _sqe->ioprio |= (ushort)IORING_RECV_MULTISHOT;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareSendMessage(FileDescriptor fd, msghdr* msg, uint flags)
    {
        PrepareReadWrite(IoUringOp.SendMsg, fd, msg, 1, 0);
        _sqe->msg_flags = flags;
        _queue.NotifyPrepared(_index);
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
        _queue.NotifyPrepared(_index);
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
        _queue.NotifyPrepared(_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PreparePollUpdate(ulong oldUserData, ulong newUserData, uint pollMask, uint flags)
    {
        PrepareReadWrite(IoUringOp.PollRemove, -1, null, flags,
            newUserData);
        _sqe->addr = oldUserData;
        _sqe->poll32_events = PreparePollMask(pollMask);
        _queue.NotifyPrepared(_index);
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
            _queue.NotifyPrepared(_index);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTimeoutRemove(ulong user_data, uint flags)
    {
        PrepareReadWrite(IoUringOp.TimeoutRemove, -1, null, 0, 0);
        _sqe->addr = user_data;
        _sqe->timeout_flags = flags;
        _queue.NotifyPrepared(_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareTimeoutUpdate(ref __kernel_timespec ts, ulong user_data, uint flags)
    {
        fixed (__kernel_timespec* tsPtr = &ts)
        {
            PrepareReadWrite(IoUringOp.TimeoutRemove, -1, null, 0, (ulong)tsPtr);
            _sqe->addr = user_data;
            _sqe->timeout_flags = flags | IORING_TIMEOUT_UPDATE;
            _queue.NotifyPrepared(_index);
        }
    }

    #endregion

    #region Network

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAccept(int fd, ref sockaddr addr, ref uint addrlen, int flags)
    {
        fixed (sockaddr* addrPtr = &addr)
        {
            PrepareReadWrite(IoUringOp.Accept, fd, addrPtr, 0, (ulong)addrlen);
            _sqe->accept_flags = (uint)flags;
            _queue.NotifyPrepared(_index);
        }
    }

    /* accept directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareAcceptDirect(int fd, ref sockaddr addr, ref uint addrlen, int flags, uint file_index)
    {
        PrepareAccept(fd, ref addr, ref addrlen, flags);
        SetTargetFixedFile(file_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultishotAccept(int fd, ref sockaddr addr, ref uint addrlen, int flags)
    {

        PrepareAccept(fd, ref addr, ref addrlen, flags);
        _sqe->ioprio |= (ushort)IORING_ACCEPT_MULTISHOT;
    }

    /* multishot accept directly into the fixed file table */
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareMultishotAcceptDirect(int fd, ref sockaddr addr, ref uint addrlen, int flags)
    {
        PrepareMultishotAccept(fd, ref addr, ref addrlen, flags);
        SetTargetFixedFile(IORING_FILE_INDEX_ALLOC - 1);
    }

    #endregion

    #region Buffer

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareProvideBuffers(void* addr, int len, int nr, int bgid, int bid)
    {
        PrepareReadWrite(IoUringOp.ProvideBuffers, nr, addr, (uint)len, (ulong)bid);
        _sqe->buf_group = (ushort)bgid;
        _queue.NotifyPrepared(_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareRemoveBuffers(int nr, int bgid)
    {
        PrepareReadWrite(IoUringOp.RemoveBuffers, nr, null, 0, 0);
        _sqe->buf_group = (ushort)bgid;
        _queue.NotifyPrepared(_index);
    }

    #endregion

    #region Misc

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFSync(FileDescriptor fd, uint fsyncFlags)
    {
        PrepareReadWrite(IoUringOp.FSync, fd, null, 0, 0);
        _sqe->fsync_flags = fsyncFlags;
        _queue.NotifyPrepared(_index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareNop(ulong userData = 0, SqeOption options = SqeOption.None, ushort personality = 0)
    {
        unchecked
        {
            _sqe->opcode = (byte)IoUringOp.Nop;
            _sqe->flags = (byte)options;
            _sqe->fd = -1;
            _sqe->user_data = userData;
            _sqe->personality = personality;
        }

        _queue.NotifyPrepared(_index);
    }

    #endregion
}