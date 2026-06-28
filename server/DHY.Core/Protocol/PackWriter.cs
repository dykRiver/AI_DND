using System.Buffers.Binary;
using System.Text;
using DHY.Core.Protocol;

/// <summary>
/// 消息写入器
/// </summary>
public ref struct PackWriter
{
    private BufferWriter writer;

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buffer">内存块</param>
    /// <param name="version">版本号</param>
    public PackWriter(Span<byte> buffer, string version = default)
    {
        this.writer = new BufferWriter(buffer);
        Version = version;
    }

    /// <summary>
    /// 编码后的数组
    /// </summary>
    /// <returns></returns>
    public byte[] FlushAndGetEncodingArray()
    {
        return writer.Written.Slice(writer.BeforeCodingWrittenPosition).ToArray();
    }

    /// <summary>
    /// 编码后的内存块
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> FlushAndGetEncodingReadOnlySpan()
    {
        return writer.Written.Slice(writer.BeforeCodingWrittenPosition);
    }

    /// <summary>
    /// 获取实际写入的内存块
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> FlushAndGetRealReadOnlySpan()
    {
        return writer.Written;
    }

    /// <summary>
    /// 获取实际写入的数组
    /// </summary>
    /// <returns></returns>
    public byte[] FlushAndGetRealArray()
    {
        return writer.Written.ToArray();
    }

    /// <summary>
    /// 写入头标识
    /// </summary>
    public void WriteStart() => WriteByte(Packet.BeginFlag);

    /// <summary>
    /// 写入尾标识
    /// </summary>
    public void WriteEnd() => WriteByte(Packet.EndFlag);

    /// <summary>
    /// 写入空标识,0x00
    /// </summary>
    /// <param name="position"></param>
    public void Nil(out int position)
    {
        position = writer.WrittenCount;
        var span = writer.Free;
        span[0] = 0x00;
        writer.Advance(1);
    }

    /// <summary>
    /// 跳过多少字节数
    /// </summary>
    /// <param name="count"></param>
    /// <param name="position">跳过前的内存位置</param>
    public void Skip(in int count, out int position)
    {
        position = writer.WrittenCount;
        var span = writer.Free;
        for (var i = 0; i < count; i++)
        {
            span[i] = 0x00;
        }
        writer.Advance(count);
    }

    /// <summary>
    /// 跳过多少字节数
    /// </summary>
    /// <param name="count"></param>
    /// <param name="position">跳过前的内存位置</param>
    /// <param name="fullValue">用什么数值填充跳过的内存块</param>
    public void Skip(in int count, out int position, in byte fullValue = 0x00)
    {
        position = writer.WrittenCount;
        var span = writer.Free;
        for (var i = 0; i < count; i++)
        {
            span[i] = fullValue;
        }
        writer.Advance(count);
    }

    /// <summary>
    /// 写入一个字符
    /// </summary>
    /// <param name="value"></param>
    public void WriteChar(in char value)
    {
        var span = writer.Free;
        span[0] = (byte)value;
        writer.Advance(1);
    }

    /// <summary>
    /// 写入一个字节
    /// </summary>
    /// <param name="value"></param>
    public void WriteByte(in byte value)
    {
        var span = writer.Free;
        span[0] = value;
        writer.Advance(1);
    }

    /// <summary>
    /// 写入两个字节的有符号数值类型
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt16(in short value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(writer.Free, value);
        writer.Advance(2);
    }

    /// <summary>
    /// 写入两个字节的无符号数值类型
    /// </summary>
    /// <param name="value"></param>
    public void WriteUInt16(in ushort value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(writer.Free, value);
        writer.Advance(2);
    }

    /// <summary>
    /// 写入四个字节的有符号数值类型
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt32(in int value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(writer.Free, value);
        writer.Advance(4);
    }

    /// <summary>
    /// 写入四个字节的无符号数值类型
    /// </summary>
    /// <param name="value"></param>
    public void WriteUInt32(in uint value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(writer.Free, value);
        writer.Advance(4);
    }

    /// <summary>
    /// 写入八个字节的无符号数值类型
    /// </summary>
    /// <param name="value"></param>
    public void WriteUInt64(in ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(writer.Free, value);
        writer.Advance(8);
    }

    /// <summary>
    /// 写入八个字节的有符号数值类型
    /// </summary>
    /// <param name="value"></param>
    public void WriteInt64(in long value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(writer.Free, value);
        writer.Advance(8);
    }

    /// <summary>
    /// 写入字符串
    /// </summary>
    /// <param name="value"></param>
    public void WriteString(in string value)
    {
        byte[] codeBytes = PacketConst.Encoding.GetBytes(value);
        codeBytes.CopyTo(writer.Free);
        writer.Advance(codeBytes.Length);
    }

    /// <summary>
    /// 写入指定长度字符串，不足长度补0
    /// </summary>
    /// <param name="value"></param>
    /// <param name="length"></param>
    public void WriteString(in string value, int length)
    {
        byte[] codeBytes = PacketConst.Encoding.GetBytes(value);
        codeBytes.CopyTo(writer.Free);

        if (codeBytes.Length < length)
        {
            writer.Advance(length);
        }
        else
        {
            writer.Advance(codeBytes.Length);
        }
    }

    /// <summary>
    /// 写入数组
    /// </summary>
    /// <param name="src"></param>
    public void WriteArray(in ReadOnlySpan<byte> src)
    {
        src.CopyTo(writer.Free);
        writer.Advance(src.Length);
    }

    /// <summary>
    /// 根据内存定位,反写两个字节的无符号数值类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteUInt16Return(in ushort value, in int position)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(writer.Written.Slice(position, 2), value);
    }

    /// <summary>
    /// 根据内存定位,反写两个字节的有符号数值类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteInt16Return(in short value, in int position)
    {
        BinaryPrimitives.WriteInt16LittleEndian(writer.Written.Slice(position, 2), value);
    }

    /// <summary>
    /// 根据内存定位,反写四个字节的有符号数值类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteInt32Return(in int value, in int position)
    {
        BinaryPrimitives.WriteInt32LittleEndian(writer.Written.Slice(position, 4), value);
    }

    /// <summary>
    /// 根据内存定位,反写四个字节的无符号数值类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteUInt32Return(in uint value, in int position)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(writer.Written.Slice(position, 4), value);
    }

    /// <summary>
    /// 根据内存定位,反写八个字节的有符号数值类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteInt64Return(in long value, in int position)
    {
        BinaryPrimitives.WriteInt64LittleEndian(writer.Written.Slice(position, 8), value);
    }

    /// <summary>
    /// 根据内存定位,反写八个字节的无符号数值类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteUInt64Return(in ulong value, in int position)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(writer.Written.Slice(position, 8), value);
    }

    /// <summary>
    /// 根据内存定位,反写1个字节的数值类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteByteReturn(in byte value, in int position)
    {
        writer.Written[position] = value;
    }

    /// <summary>
    /// 根据内存定位,反写一串字符串数据
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position"></param>
    public void WriteStringReturn(in string value, in int position)
    {
        Span<byte> codeBytes = PacketConst.Encoding.GetBytes(value);
        codeBytes.CopyTo(writer.Written.Slice(position));
    }

    /// <summary>
    /// 根据内存定位,反写一组数组数据
    /// </summary>
    /// <param name="src"></param>
    /// <param name="position"></param>
    public void WriteArrayReturn(in ReadOnlySpan<byte> src, in int position)
    {
        src.CopyTo(writer.Written.Slice(position));
    }


    /// <summary>
    /// 写入八个字节的日期类型
    /// </summary>
    /// <param name="value"></param>
    /// <param name="fromBase"></param>
    public void WriteDateTime(in DateTime value, in int fromBase = 16)
    {
        //BinaryPrimitives.WriteInt64LittleEndian(writer.Free, value);
        writer.Advance(8);
        throw new NotImplementedException("未确定时间传输的方式");
    }

    /// <summary>
    /// 将指定内存块进行或运算并写入一个字节
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    public void WriteXor(in int start, in int end)
    {
        if (start > end)
        {
            throw new ArgumentOutOfRangeException($"start>end:{start}>{end}");
        }
        var xorSpan = writer.Written.Slice(start, end);
        byte result = xorSpan[0];
        for (int i = start + 1; i < end; i++)
        {
            result = (byte)(result ^ xorSpan[i]);
        }
        var span = writer.Free;
        span[0] = result;
        writer.Advance(1);
    }

    /// <summary>
    /// 将指定内存块进行或运算并写入一个字节
    /// </summary>
    /// <param name="start"></param>
    public void WriteXor(in int start)
    {
        if (writer.WrittenCount < start)
        {
            throw new ArgumentOutOfRangeException($"Written<start:{writer.WrittenCount}>{start}");
        }
        var xorSpan = writer.Written.Slice(start);
        byte result = xorSpan[0];
        for (int i = start + 1; i < xorSpan.Length; i++)
        {
            result = (byte)(result ^ xorSpan[i]);
        }
        var span = writer.Free;
        span[0] = result;
        writer.Advance(1);
    }

    /// <summary>
    /// 将内存块进行或运算并写入一个字节
    /// </summary>
    public void WriteXor()
    {
        if (writer.WrittenCount < 1)
        {
            throw new ArgumentOutOfRangeException($"Written<start:{writer.WrittenCount}>{1}");
        }
        //从第1位开始
        var xorSpan = writer.Written.Slice(1);
        byte result = xorSpan[0];
        for (int i = 1; i < xorSpan.Length; i++)
        {
            result = (byte)(result ^ xorSpan[i]);
        }
        var span = writer.Free;
        span[0] = result;
        writer.Advance(1);
    }

    /// <summary>
    /// 写入Hex编码数据
    /// </summary>
    /// <param name="value"></param>
    /// <param name="len"></param>
    public void WriteHex(string value, in int len)
    {
        value = value ?? "";
        value = value.Replace(" ", "");
        int startIndex = 0;
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            startIndex = 2;
        }
        int length = len;
        if (length == -1)
        {
            length = (value.Length - startIndex) / 2;
        }
        int noOfZero = length * 2 + startIndex - value.Length;
        if (noOfZero > 0)
        {
            value = value.Insert(startIndex, new string('0', noOfZero));
        }
        int byteIndex = 0;
        var hexSpan = value.AsSpan();
        var spanFree = writer.Free;
        while (startIndex < value.Length && byteIndex < length)
        {
            spanFree[byteIndex++] = Convert.ToByte(hexSpan.Slice(startIndex, 2).ToString(), 16);
            startIndex += 2;
        }
        writer.Advance(byteIndex);
    }

    /// <summary>
    /// 写入ASCII编码数据
    /// </summary>
    /// <param name="value"></param>
    public void WriteASCII(in string value)
    {
        var spanFree = writer.Free;
        var bytes = Encoding.ASCII.GetBytes(value).AsSpan();
        bytes.CopyTo(spanFree);
        writer.Advance(bytes.Length);
    }

    /// <summary>
    /// 将内存块进行转义处理
    /// </summary>
    public void WriteFullEncode()
    {
        //TODO:指令中包含 0x5A或者0xA5的转义处理
    }

    /// <summary>
    /// 将字符串写入并写入一个\0作为结尾
    /// </summary>
    /// <param name="value"></param>
    public void WriteStringEndChar0(string value)
    {
        WriteString(value);
        WriteChar('\0');
    }

    /// <summary>
    /// 获取当前内存块写入的位置
    /// </summary>
    /// <returns></returns>
    public int GetCurrentPosition()
    {
        return writer.WrittenCount;
    }

    [Obsolete("已弃用，CRC应在协议中进行计算后加密")]
    public void WriteCrc16()
    {
        var calculateCheckXorCode = CRC16.Calc(writer.Written.ToArray());
        WriteInt32(calculateCheckXorCode);
    }
}