using System.Buffers.Binary;
using System.Text;

/// <summary>
/// 消息读取器
/// </summary>
public ref struct PackReader
{
    /// <summary>
    /// 读取buffer
    /// </summary>
    public ReadOnlySpan<byte> Reader { get; private set; }

    /// <summary>
    /// 原数据
    /// </summary>
    public ReadOnlySpan<byte> SrcBuffer { get; }

    /// <summary>
    /// 读取到的数量
    /// </summary>
    public int ReaderCount { get; private set; }

    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; set; }
    private int _calculateCheckXorCode;
    private int _realCheckXorCode;
    private bool _checkXorCodeVali;
    /// <summary>
    /// 是否进行解码操作
    /// 若进行解码操作，则对应的是一个正常的包
    /// 若不进行解码操作，则对应的是一个非正常的包（头部包，数据体包等等）
    /// 主要用来一次性读取所有数据体内容操作
    /// </summary>
    private bool _decoded;

    /// <summary>
    /// 解码（转义还原）,计算校验和
    /// </summary>
    /// <param name="srcBuffer"></param>
    /// <param name="version">默认Version</param>
    public PackReader(ReadOnlySpan<byte> srcBuffer, string version = default)
    {
        SrcBuffer = srcBuffer;
        ReaderCount = 0;
        _realCheckXorCode = 0x00;
        _calculateCheckXorCode = 0x00;
        _checkXorCodeVali = false;
        _decoded = false;
        Version = version;
        Reader = srcBuffer;
    }

    /// <summary>
    /// 校验CRC16
    /// </summary>
    /// <returns></returns>
    public void Crc16Check(int crcDecrypt)
    {
        // 校验码之前的数据体位置
        var msgContentWhitoutCrc16Position = ReaderCount - 4;
        //传入的crc校验码
        _realCheckXorCode = crcDecrypt;
        var checkBodies = SrcBuffer[..msgContentWhitoutCrc16Position];
        _calculateCheckXorCode = CRC16.Calc(checkBodies.ToArray());
        _checkXorCodeVali = _calculateCheckXorCode == _realCheckXorCode;
        _decoded = true;
    }

    /// <summary>
    /// 计算的校验码
    /// </summary>
    public int CalculateCheckXorCode => _calculateCheckXorCode;

    /// <summary>
    /// 实际获取的校验码
    /// </summary>
    public int RealCheckXorCode => _realCheckXorCode;

    /// <summary>
    /// 验证码是否正确
    /// </summary>
    public bool CheckXorCodeVali => _checkXorCodeVali;

    /// <summary>
    /// 读取标识头
    /// </summary>
    /// <returns></returns>
    public byte ReadStart() => ReadByte();

    /// <summary>
    /// 读取尾标识
    /// </summary>
    /// <returns></returns>
    public byte ReadEnd() => ReadByte();

    /// <summary>
    /// 读取有符号位的两字节数值类型
    /// </summary>
    /// <returns></returns>
    public short ReadInt16()
    {
        return BinaryPrimitives.ReadInt16LittleEndian(GetReadOnlySpan(2));
    }

    /// <summary>
    /// 读取无符号位的两字节数值类型
    /// </summary>
    /// <returns></returns>
    public ushort ReadUInt16()
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(GetReadOnlySpan(2));
    }

    /// <summary>
    /// 读取无符号位的四字节数值类型
    /// </summary>
    /// <returns></returns>
    public uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(GetReadOnlySpan(4));
    }

    /// <summary>
    /// 读取有符号位的四字节数值类型
    /// </summary>
    /// <returns></returns>
    public int ReadInt32()
    {
        return BinaryPrimitives.ReadInt32LittleEndian(GetReadOnlySpan(4));
    }

    /// <summary>
    /// 读取无符号位的八字节数值类型
    /// </summary>
    /// <returns></returns>
    public ulong ReadUInt64()
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(GetReadOnlySpan(8));
    }

    /// <summary>
    /// 读取有符号位的八字节数值类型
    /// </summary>
    /// <returns></returns>
    public long ReadInt64()
    {
        return BinaryPrimitives.ReadInt64LittleEndian(GetReadOnlySpan(8));
    }

    /// <summary>
    /// 读取一个字节
    /// </summary>
    /// <returns></returns>
    public byte ReadByte()
    {
        return GetReadOnlySpan(1)[0];
    }

    /// <summary>
    /// 读取一个字符
    /// </summary>
    /// <returns></returns>
    public char ReadChar()
    {
        return (char)GetReadOnlySpan(1)[0];
    }

    /// <summary>
    /// 虚拟读取一个字节，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public byte ReadVirtualByte()
    {
        return GetVirtualReadOnlySpan(1)[0];
    }

    /// <summary>
    /// 虚拟读取一个数组，不计入内存偏移量
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> ReadVirtualArray(int count)
    {
        return GetVirtualReadOnlySpan(count);
    }

    /// <summary>
    /// 虚拟读取无符号位的两字节数值类型，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public ushort ReadVirtualUInt16()
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(GetVirtualReadOnlySpan(2));
    }

    /// <summary>
    /// 虚拟读取有符号位的两字节数值类型，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public short ReadVirtualInt16()
    {
        return BinaryPrimitives.ReadInt16LittleEndian(GetVirtualReadOnlySpan(2));
    }

    /// <summary>
    /// 虚拟读取无符号位的四字节数值类型，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public uint ReadVirtualUInt32()
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(GetVirtualReadOnlySpan(4));
    }

    /// <summary>
    /// 虚拟读取有符号位的四字节数值类型，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public int ReadVirtualInt32()
    {
        return BinaryPrimitives.ReadInt32LittleEndian(GetVirtualReadOnlySpan(4));
    }

    /// <summary>
    /// 虚拟读取无符号位的八字节数值类型，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public ulong ReadVirtualUInt64()
    {
        return BinaryPrimitives.ReadUInt64LittleEndian(GetVirtualReadOnlySpan(8));
    }

    /// <summary>
    /// 虚拟读取有符号位的八字节数值类型，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public long ReadVirtualInt64()
    {
        return BinaryPrimitives.ReadInt64LittleEndian(GetVirtualReadOnlySpan(8));
    }

    /// <summary>
    /// 读取数字编码 
    /// 大端模式、高位在前
    /// </summary>
    /// <param name="len"></param>
    public string ReadBigNumber(int len)
    {
        ulong result = 0;
        var readOnlySpan = GetReadOnlySpan(len);
        for (int i = 0; i < len; i++)
        {
            ulong currentData = (ulong)readOnlySpan[i] << (8 * (len - i - 1));
            result += currentData;
        }
        return result.ToString();
    }

    /// <summary>
    /// 读取固定大小的内存块
    /// </summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> ReadArray(int len)
    {
        return GetReadOnlySpan(len).Slice(0, len);
    }

    /// <summary>
    /// 读取固定大小的内存块
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> ReadArray(int start, int end)
    {
        return Reader.Slice(start, end);
    }

    /// <summary>
    /// 读取UTF-8字符串编码
    /// </summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public string ReadString(int len)
    {
        var readOnlySpan = GetReadOnlySpan(len);
        string value = PacketConst.Encoding.GetString(readOnlySpan.Slice(0, len).ToArray());
        return value.Trim('\0');
    }

    /// <summary>
    /// 读取ASCII编码
    /// </summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public string ReadASCII(int len)
    {
        var readOnlySpan = GetReadOnlySpan(len);
        string value = Encoding.ASCII.GetString(readOnlySpan.Slice(0, len).ToArray());
        return value;
    }

    /// <summary>
    /// 读取剩余数据体内容为字符串模式
    /// </summary>
    /// <returns></returns>
    public string ReadRemainStringContent()
    {
        return ReadString(ReadCurrentRemainContentLength());
    }

    /// <summary>
    /// 读取数量大小的内存块
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    private ReadOnlySpan<byte> GetReadOnlySpan(int count)
    {
        ReaderCount += count;
        return Reader.Slice(ReaderCount - count);
    }

    /// <summary>
    /// 虚拟读取数量大小的内存块，不计入内存偏移量
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetVirtualReadOnlySpan(int count)
    {
        return Reader.Slice(ReaderCount, count);
    }

    /// <summary>
    /// 读取数据体内存块
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> ReadContent(int count = 0)
    {
        if (_decoded)
        {
            //内容长度=总长度-读取的长度-5（校验码4位+终止符1位）
            int totalContent = Reader.Length - ReaderCount - 5;
            //实际读取内容长度
            int realContent = totalContent - count;
            int tempReaderCount = ReaderCount;
            ReaderCount += realContent;
            return Reader.Slice(tempReaderCount, realContent);
        }
        else
        {
            return Reader.Slice(ReaderCount);
        }
    }

    /// <summary>
    /// 读取一整串字符串到\0结束
    /// </summary>
    /// <returns></returns>
    public string ReadStringEndChar0()
    {
        var remainSpans = Reader.Slice(ReaderCount, ReadCurrentRemainContentLength());
        int length = remainSpans.IndexOf((byte)'\0') + 1;
        string value = PacketConst.Encoding.GetString(ReadArray(length).ToArray());
        return value.Trim('\0');
    }

    /// <summary>
    /// 虚拟读取一整串字符串到\0结束，不计入内存偏移量
    /// </summary>
    /// <returns></returns>
    public string ReadVirtualStringEndChar0()
    {
        var remainSpans = Reader.Slice(ReaderCount);
        string value = PacketConst.Encoding.GetString(GetVirtualReadOnlySpan(remainSpans.IndexOf((byte)'\0') + 1).ToArray());
        return value.Trim('\0');
    }

    /// <summary>
    /// 读取剩余数据体内容长度
    /// </summary>
    /// <returns></returns>
    public int ReadCurrentRemainContentLength()
    {
        if (_decoded)
        {
            //内容长度=总长度-读取的长度-5（校验码4位+终止符1位）
            return Reader.Length - ReaderCount - 5;
        }
        else
        {
            return Reader.Length - ReaderCount;
        }
    }

    /// <summary>
    /// 跳过多少字节
    /// </summary>
    /// <param name="count"></param>
    public void Skip(int count = 1)
    {
        ReaderCount += count;
    }
}