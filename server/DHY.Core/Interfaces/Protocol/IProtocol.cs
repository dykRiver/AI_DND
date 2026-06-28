/// <summary>
/// 自定义结构通讯协议接口
/// </summary>
public interface IProtocol
{
    /// <summary>
    /// 协议名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 所属协议族；
    /// </summary>
    int Family { get; }

    /// <summary>
    /// 协议版本
    /// </summary>
    string Version { get; }

    /// <summary>
    /// 传入网络数据包，确认是否接受处理
    /// </summary>
    /// <param name="buffer">网络协议包</param>
    /// <returns></returns>
    IProtocol Accept<TPacketData>(ArraySegment<byte> buffer) where TPacketData : DeviceCommand;

    /// <summary>
    /// 用数据域的数据直接封包
    /// </summary>
    /// <param name="packData"></param>
    /// <returns></returns>
    ArraySegment<byte> CreatePacket<TPacketData>(TPacketData packData) where TPacketData : DeviceCommand;

    /// <summary>
    /// 用数据域的数据生成回复包，包序号使用packData参数中的<see cref="DeviceCommand.PackNo"/>
    /// </summary>
    /// <param name="packData"></param>
    /// <returns></returns>
    ArraySegment<byte> CreateResponsePacket<TPacketData>(TPacketData packData) where TPacketData : DeviceCommand;

    /// <summary>
    /// 用数据域的数据根据最大长度限制封成多个包
    /// </summary>
    /// <typeparam name="TMultiPacketData"></typeparam>
    /// <param name="packData"></param>
    /// <returns></returns>
    IEnumerable<ArraySegment<byte>> CreateMultiplePacket<TMultiPacketData>(TMultiPacketData packData) where TMultiPacketData : MultiPackCommand;

    /// <summary>
    /// 创建一个指定类型的使用默认值的包
    /// </summary>
    /// <typeparam name="TPacketData"></typeparam>
    /// <param name="addressDomain"></param>
    /// <returns></returns>
    ArraySegment<byte> CreateEmptyPacket<TPacketData>(ushort addressDomain) where TPacketData : DeviceCommand;

    /// <summary>
    /// 从指定数据解析出数据包并从数据包解析出数据域对象
    /// </summary>
    /// <typeparam name="TPacketData"></typeparam>
    /// <returns></returns>
    TPacketData TakeSinglePackData<TPacketData>(ArraySegment<byte> buffer) where TPacketData : DeviceCommand;

    /// <summary>
    /// 从缓冲区组合数据包解析出所有数据域对象，完整包的<see cref="MultiPackCommand.IsFullPack"/> 值为<c>true</c>
    /// </summary>
    /// <typeparam name="TPacketData"></typeparam>
    /// <returns>完整数据包，部分数据包</returns>
    IEnumerable<TPacketData> TakeAllMultiplePackData<TPacketData>() where TPacketData : MultiPackCommand;

    /// <summary>
    /// 从缓冲区组合数据包解析出一个数据域对象，没有接收完整之前不会返回数据
    /// </summary>
    /// <typeparam name="TPacketData"></typeparam>
    /// <returns>完整数据包</returns>
    TPacketData WaitOneMultipleData<TPacketData>() where TPacketData : MultiPackCommand;
}