using System.Buffers;

namespace DHY.Core.Utils;

public class DataBuffer<TData>
{
    private static readonly object _lock = new();
    private static ArraySegment<byte> _buffer = new ArraySegment<byte>(Array.Empty<byte>());

    public static void Accept(ArraySegment<byte> buffer)
    {
        if (buffer == null || buffer.Array == null || buffer.Count == 0)
        {
            return;
        }

        lock (_lock)
        {
            // 计算新缓冲区的总长度
            int totalLength = _buffer.Count + buffer.Count;
            // 使用 ArrayPool 减少内存分配
            var pooledArray = ArrayPool<byte>.Shared.Rent(totalLength);

            try
            {
                // 复制现有缓冲区的内容到新数组
                Buffer.BlockCopy(_buffer.Array, _buffer.Offset, pooledArray, 0, _buffer.Count);
                // 复制新接收的缓冲区内容到新数组
                Buffer.BlockCopy(buffer.Array, buffer.Offset, pooledArray, _buffer.Count, buffer.Count);
                // 更新全局缓冲区
                _buffer = new ArraySegment<byte>(pooledArray, 0, totalLength);
            }
            catch
            {
                // 如果发生异常，确保归还租用的数组
                ArrayPool<byte>.Shared.Return(pooledArray);
                throw;
            }
        }
    }

    /// <summary>
    /// 获取当前缓冲区的内容。
    /// </summary>
    public static ArraySegment<byte> GetBuffer()
    {
        lock (_lock)
        {
            return new ArraySegment<byte>(_buffer.Array, _buffer.Offset, _buffer.Count);
        }
    }

    /// <summary>
    /// 从缓冲区的起始位置删除指定长度的内容。
    /// </summary>
    /// <param name="length">要删除的长度（以字节为单位）。</param>
    public static void RemoveFromStart(int length)
    {
        if (length <= 0)
        {
            return;
        }

        lock (_lock)
        {
            if (_buffer.Array == null || _buffer.Count == 0)
            {
                // 如果缓冲区为空或删除长度为 0，直接返回
                return;
            }

            int actualLength = Math.Min(length, _buffer.Count); // 确保不超出缓冲区范围

            if (actualLength == _buffer.Count)
            {
                // 如果删除长度等于当前缓冲区长度，直接清空缓冲区
                ClearBuffer();
                return;
            }

            // 更新缓冲区
            int remainingBytes = _buffer.Count - actualLength;
            ArraySegment<byte> updatedBuffer = new ArraySegment<byte>(
                _buffer.Array,
                _buffer.Offset + actualLength,
                remainingBytes);

            _buffer = updatedBuffer;
        }
    }

    /// <summary>
    /// 清空缓冲区。
    /// </summary>
    private static void ClearBuffer()
    {
        if (_buffer.Array != null && _buffer.Count > 0)
        {
            ArrayPool<byte>.Shared.Return(_buffer.Array);
            _buffer = new ArraySegment<byte>(Array.Empty<byte>());
        }
    }

}
