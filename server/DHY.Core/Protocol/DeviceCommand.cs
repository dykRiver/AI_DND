using DHY.Core.Interfaces.Protocol;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DHY.Protocol.SimensS7")]
[assembly: InternalsVisibleTo("DHY.Protocol")]
public abstract class DeviceCommand : IDeviceCommand
{
    /// <summary>
    /// 命令码
    /// </summary>
    public abstract byte CommandId { get; }

    /// <summary>
    /// 包序号
    /// </summary>
    public ushort PackNo { get; set; }

    /// <summary>
    /// 非数据域内容，不参与序列化和反序列化, 仅限于组装协议的时候使用，不能用作指令内容获取
    /// </summary>
    public virtual ushort DeviceNo { internal get; set; }

    /// <summary>
    /// 解析数据域内容到当前实例
    /// </summary>
    /// <param name="buffer"></param>
    internal abstract void Deserialize(ArraySegment<byte> buffer);

    /// <summary>
    /// 将当前指令封送成数据包
    /// </summary>
    /// <returns></returns>
    internal abstract byte[] Serialize();
}