using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using LibUringSharp.Enums;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp.Submission;

public readonly unsafe partial struct Submission
{
    #region Read/Write

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
            _queue.NotifyPrepared(_index);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void PrepareFSync(FileDescriptor fd, uint fsyncFlags)
    {
        PrepareReadWrite(IoUringOp.FSync, fd, null, 0, 0);
        _sqe->fsync_flags = fsyncFlags;
    }

    #region File I/O

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
        }

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
    public void PrepareWrite(FileDescriptor fd, Span<byte> buf, ulong offset)
    {
        fixed (void* bufPtr = buf)
        {
            PrepareReadWrite(IoUringOp.Write, fd, bufPtr, (uint)buf.Length, offset);
        }
    }

    #endregion

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
}