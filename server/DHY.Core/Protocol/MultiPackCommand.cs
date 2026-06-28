using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DHY.Protocol")]
[assembly: InternalsVisibleTo("DDCS.XUnitTest")]
public abstract class MultiPackCommand : DeviceCommand, ICloneable
{
    /// <summary>
    /// 后续帧Id
    /// </summary>
    public abstract byte ContinuesCommandId { get; }

    /// <summary>
    /// 帧序号
    /// </summary>
    public ushort DataPackNo { get; set; }

    /// <summary>
    /// 总帧数
    /// </summary>
    public ushort TotalPack { get; set; }

    /// <summary>
    /// 完整包
    /// </summary>
    public bool IsFullPack { get; internal set; }

    internal virtual byte[] PartialSerialize(ArraySegment<byte> dataPackPartial)
    {
        var buffer = new byte[5 + dataPackPartial.Count];
        var writer = new PackWriter(buffer);
        writer.WriteByte(ContinuesCommandId);
        writer.WriteUInt16(DataPackNo);
        writer.WriteUInt16(TotalPack);
        writer.WriteArray(dataPackPartial);

        return writer.FlushAndGetRealArray();
    }

    /// <summary>
    /// 多个包先接收部分数组，不进行解析（调用方自行保存)
    /// </summary>
    /// <param name="buffer"></param>
    internal virtual void AcceptPartial(ArraySegment<byte> buffer)
    {
        var reader = new PackReader(buffer);
        _ = reader.ReadByte();
        DataPackNo = reader.ReadUInt16();
        TotalPack = reader.ReadUInt16();
        SetDataAreaBuffer(reader.ReadArray(reader.ReadCurrentRemainContentLength()).ToArray());
    }

    /// <summary>
    /// 将收到的多包信息组合在一起反序列化
    /// </summary>
    /// <param name="buffer">完整包数据</param>
    public abstract void DeserializeFinal(ArraySegment<byte> buffer);

    /// <summary>
    /// 分包数据包内容
    /// </summary>
    /// <param name="buffer"></param>
    public abstract void SetDataAreaBuffer(ArraySegment<byte> buffer);

    /// <summary>
    /// 获取数据域数据
    /// </summary>
    /// <param name="buffer"></param>
    internal abstract ArraySegment<byte> GetDataAreaBuffer();

    public object Clone()
    {
        return MemberwiseClone();
    }
}