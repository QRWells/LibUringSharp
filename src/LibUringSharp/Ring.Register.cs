using LibUringSharp.Enums;
using LibUringSharp.Exceptions;
using Linux.Exceptions;
using Linux.Handles;
using static Linux.LibC;

namespace LibUringSharp;

public sealed unsafe partial class Ring
{
    private int RegisterRingFd()
    {
        var up = new io_uring_rsrc_update { data = _ringFd, offset = uint.MaxValue };
        var ret = io_uring_register(_ringFd, IORING_REGISTER_RING_FDS, &up, 1);
        if (ret != 1) return ret;
        _enterRingFd = new FileDescriptor(unchecked((int)up.offset));
        _intFlags |= RingInterrupt.RegRing;

        return ret;
    }

    public int UnregisterRingFd()
    {
        var up = new io_uring_rsrc_update { offset = _enterRingFd };

        var ret = io_uring_register(_ringFd, IORING_UNREGISTER_RING_FDS, &up, 1);
        if (ret != 1) return ret;
        _enterRingFd = _ringFd;
        _intFlags &= ~RingInterrupt.RegRing;

        return ret;
    }

    internal int RegisterProbe(io_uring_probe* p, uint nrOps)
    {
        return io_uring_register(_ringFd, IORING_REGISTER_PROBE, p, nrOps);
    }

    public int RegisterPersonality()
    {
        return io_uring_register(_ringFd, IORING_REGISTER_PERSONALITY, null, 0);
    }

    public int UnregisterPersonality(int id)
    {
        return io_uring_register(_ringFd, IORING_UNREGISTER_PERSONALITY, null, (uint)id);
    }

    public int RegisterRestrictions(Span<io_uring_restriction> res)
    {
        fixed (io_uring_restriction* resPtr = res)
        {
            return io_uring_register(_ringFd, IORING_REGISTER_RESTRICTIONS, resPtr, (uint)res.Length);
        }
    }

    public int EnableRings()
    {
        return io_uring_register(_ringFd, IORING_REGISTER_ENABLE_RINGS, null, 0);
    }

    /// <summary>
    ///     Register a buffer ring to the ring.
    ///     If the ring with the same id is already registered, the old ring will be unregistered.
    /// </summary>
    /// <param name="bufferRing"></param>
    /// <exception cref="Exception"></exception>
    public void RegisterBufferRing(ref BufferRing bufferRing)
    {
        if (_bufferRings.ContainsKey(bufferRing.Id))
            UnregisterBufferRing(bufferRing.Id);

        var bufReg = new io_uring_buf_reg
        {
            bgid = (ushort)bufferRing.Id,
            ring_addr = bufferRing.RingAddress,
            ring_entries = bufferRing.Entries
        };
        var res = io_uring_register(_ringFd, IORING_REGISTER_PBUF_RING, &bufReg, 1);
        if (res != 0) throw new Exception("Failed to register buffer ring");
        _bufferRings.Add(bufferRing.Id, bufferRing);
    }

    public void UnregisterBufferRing(int groupId)
    {
        if (!_bufferRings.ContainsKey(groupId)) return;
        var reg = new io_uring_buf_reg { bgid = (ushort)groupId };
        var res = io_uring_register(_ringFd, IORING_UNREGISTER_PBUF_RING, &reg, 1);
        if (res != 0) throw new Exception("Failed to unregister buffer ring");
        _bufferRings.Remove(groupId);
    }

    /// <summary>
    ///     Register a buffer group to the ring.
    /// </summary>
    /// <param name="bufferSize">Size of each buffer.</param>
    /// <param name="bufferCount">Number of buffers.</param>
    /// <returns>Group id for the buffer group.</returns>
    public Task<int> RegisterBufferGroupAsync(uint bufferSize, uint bufferCount)
    {
        var id = _lastGroupId++;
        var bufferGroup = new BufferGroup(bufferSize, bufferCount);
        var tcs = new TaskCompletionSource<int>();

        Issue(sqe =>
        {
            sqe.PrepareProvideBuffers(bufferGroup.Base, (int)bufferGroup.BufferSize, (int)bufferGroup.BufferCount,
                id, 0);
            Prepared(sqe);
            SubmitAndWait(1);

            _bufferGroups.Add(id, bufferGroup);
            tcs.SetResult(id);
        });

        return tcs.Task;
    }

    /// <summary>
    ///     Unregister a buffer group from the ring. If the buffer group is still in use, the operation will fail.
    ///     If the buffer group does not exist, the operation will be ignored.
    /// </summary>
    /// <param name="bufferGroupId">Group id of the buffer group.</param>
    public void UnregisterBufferGroup(int bufferGroupId)
    {
        if (!_bufferGroups.TryGetValue(bufferGroupId, out var bufferGroup)) return;

        _bufferGroups.Remove(bufferGroupId);

        Issue(sqe =>
        {
            sqe.PrepareRemoveBuffers((int)bufferGroup.BufferCount, bufferGroupId);
            Prepared(sqe);
            SubmitAndWait(1);
        });
    }

    public int RegisterSyncCancel(ref io_uring_sync_cancel_reg reg)
    {
        fixed (io_uring_sync_cancel_reg* regPtr = &reg)
        {
            return io_uring_register(_ringFd, IORING_REGISTER_SYNC_CANCEL, regPtr, 1);
        }
    }

    public int RegisterFileAllocRange(uint off, uint len)
    {
        var range = new io_uring_file_index_range { off = off, len = len };
        return io_uring_register(_ringFd, IORING_REGISTER_FILE_ALLOC_RANGE, &range, 0);
    }

    #region Buffers

    public int RegisterBuffers(Span<iovec> ioVectors)
    {
        fixed (iovec* ptr = ioVectors)
        {
            return io_uring_register(_ringFd, IORING_REGISTER_BUFFERS, ptr, (uint)ioVectors.Length);
        }
    }

    public int RegisterBuffersTags(Span<iovec> ioVectors, ref ulong tags)
    {
        fixed (iovec* ptr = ioVectors)
        {
            fixed (ulong* tagsPtr = &tags)
            {
                var reg = new io_uring_rsrc_register
                {
                    nr = (uint)ioVectors.Length,
                    data = (ulong)ptr,
                    tags = (ulong)tagsPtr
                };
                return io_uring_register(_ringFd, IORING_REGISTER_BUFFERS2,
                    &reg, io_uring_rsrc_register.Size);
            }
        }
    }

    public int RegisterBuffersUpdateTag(uint off, Span<iovec> ioVectors, ref ulong tags)
    {
        fixed (iovec* ptr = ioVectors)
        {
            fixed (ulong* tagsPtr = &tags)
            {
                var reg = new io_uring_rsrc_update2
                {
                    nr = (uint)ioVectors.Length,
                    data = (ulong)ptr,
                    tags = (ulong)tagsPtr,
                    offset = off
                };
                return io_uring_register(_ringFd, IORING_REGISTER_BUFFERS_UPDATE,
                    &reg, io_uring_rsrc_update2.Size);
            }
        }
    }

    public int RegisterBuffersSparse(uint nr)
    {
        var reg = new io_uring_rsrc_register
        {
            flags = IORING_RSRC_REGISTER_SPARSE,
            nr = nr
        };
        return io_uring_register(_ringFd, IORING_REGISTER_BUFFERS2, &reg, io_uring_rsrc_register.Size);
    }

    public int UnregisterBuffers()
    {
        return io_uring_register(_ringFd, IORING_UNREGISTER_BUFFERS, null, 0);
    }

    #endregion

    #region Files

    public int RegisterFiles(Span<FileDescriptor> files)
    {
        int ret, didIncrease = 0;
        fixed (int* filesPtr = files.ToIntSpan())
        {
            do
            {
                ret = io_uring_register(_ringFd, IORING_REGISTER_FILES, filesPtr,
                    (uint)files.Length);
                if (ret >= 0)
                    break;
                if (ret == -(int)ErrorNo.TooManyOpenFiles && didIncrease == 0)
                {
                    didIncrease = 1;
                    IncreaseResourceLimitFile((uint)files.Length);
                    continue;
                }

                break;
            } while (true);
        }

        return ret;
    }

    public int RegisterFilesTags(Span<FileDescriptor> files, ref ulong tags)
    {
        int ret;
        fixed (int* filesPtr = files.ToIntSpan())
        {
            fixed (ulong* tagsPtr = &tags)
            {
                var reg = new io_uring_rsrc_register
                {
                    nr = (uint)files.Length,
                    data = (ulong)filesPtr,
                    tags = (ulong)tagsPtr
                };

                var didIncrease = 0;

                do
                {
                    ret = io_uring_register(_ringFd, IORING_REGISTER_FILES2, &reg,
                        io_uring_rsrc_register.Size);
                    if (ret >= 0)
                        break;
                    if (ret == -(int)ErrorNo.TooManyOpenFiles && didIncrease == 0)
                    {
                        didIncrease = 1;
                        IncreaseResourceLimitFile((uint)files.Length);
                        continue;
                    }

                    break;
                } while (true);
            }
        }

        return ret;
    }

    public int RegisterFilesSparse(uint nr)
    {
        var reg = new io_uring_rsrc_register
        {
            flags = IORING_RSRC_REGISTER_SPARSE,
            nr = nr
        };
        int ret, didIncrease = 0;

        do
        {
            ret = io_uring_register(_ringFd, IORING_REGISTER_FILES2, &reg, io_uring_rsrc_register.Size);

            if (ret >= 0)
                break;
            if (ret == -(int)ErrorNo.TooManyOpenFiles && didIncrease == 0)
            {
                didIncrease = 1;
                IncreaseResourceLimitFile(nr);
                continue;
            }

            break;
        } while (true);

        return ret;
    }

    public int RegisterFilesUpdateTag(uint off, Span<FileDescriptor> files, ref ulong tags)
    {
        fixed (int* filesPtr = files.ToIntSpan())
        {
            fixed (ulong* tagsPtr = &tags)
            {
                var up = new io_uring_rsrc_update2
                {
                    offset = off,
                    data = (ulong)filesPtr,
                    tags = (ulong)tagsPtr,
                    nr = (uint)files.Length
                };

                return io_uring_register(_ringFd, IORING_REGISTER_FILES_UPDATE2,
                    &up, io_uring_rsrc_update2.Size);
            }
        }
    }

    public int UnregisterFiles()
    {
        return io_uring_register(_ringFd, IORING_UNREGISTER_FILES, null, 0);
    }

    public int RegisterFilesUpdate(uint off, Span<FileDescriptor> files)
    {
        fixed (int* filesPtr = files.ToIntSpan())
        {
            var up = new io_uring_rsrc_update
            {
                offset = off,
                data = (ulong)filesPtr
            };

            return io_uring_register(_ringFd, IORING_REGISTER_FILES_UPDATE, &up, (uint)files.Length);
        }
    }

    #endregion

    #region eventfd

    public void RegisterEventFd(FileDescriptor fd)
    {
        int fileDescriptor = fd;
        var ret = io_uring_register(_ringFd, IORING_REGISTER_EVENTFD, &fileDescriptor, 1);

        if (ret < 0) throw new RegisterEventFdFailedException();
    }

    public void RegisterEventFdAsync(FileDescriptor fd)
    {
        int fileDescriptor = fd;
        var ret = io_uring_register(_ringFd, IORING_REGISTER_EVENTFD_ASYNC, &fileDescriptor, 1);
        if (ret < 0) throw new RegisterEventFdFailedException();
    }

    public void UnregisterEventFd()
    {
        var ret = io_uring_register(_ringFd, IORING_UNREGISTER_EVENTFD, null, 1);
        if (ret < 0) throw new RegisterEventFdFailedException();
    }

    #endregion

    #region IoWorkerQueue

    public int RegisterIoWqAff(ulong cpuSize, ref cpu_set_t mask)
    {
        if (cpuSize >= 1U << 31)
            throw new ArgumentException("cpuSize must be less than 1U << 31", nameof(cpuSize));
        fixed (cpu_set_t* maskPtr = &mask)
        {
            return io_uring_register(_ringFd, IORING_REGISTER_IOWQ_AFF, maskPtr, (uint)cpuSize);
        }
    }

    public int UnregisterIoWqAff()
    {
        return io_uring_register(_ringFd, IORING_UNREGISTER_IOWQ_AFF, null, 0);
    }

    public int RegisterIoWqMaxWorkers(ref uint val)
    {
        fixed (uint* valPtr = &val)
        {
            return io_uring_register(_ringFd, IORING_REGISTER_IOWQ_MAX_WORKERS, valPtr, 2);
        }
    }

    #endregion
}