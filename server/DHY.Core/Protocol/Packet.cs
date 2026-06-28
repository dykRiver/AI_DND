
using DHY.Core.Interfaces.Protocol;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DHY.Protocol")]
namespace DHY.Core.Protocol
{
    /// <summary>
    /// 数据包基类
    /// </summary>
    public abstract class Packet : IPacket
    {
        /// <summary>
        /// 帧头
        /// </summary>
        public const byte BeginFlag = 0X5A;

        /// <summary>
        /// 帧尾
        /// </summary>
        public const byte EndFlag = 0XA5;

        /// <summary>
        /// 协议包内容最大长度
        /// </summary>
        public const byte PackDataMaxSize = 240;

        /// <summary>
        /// 数据包最小长度
        /// </summary>
        public const byte MinDataLength = 12;

        /// <summary>
        /// 数据包原始数据
        /// </summary>
        protected ArraySegment<byte> Data { get; set; }

        /// <summary>
        /// 是否校验通过
        /// </summary>
        public bool Checked { get; protected set; }

        /// <summary>
        /// 无参构造函数，用于数据包传输序列化反序列化
        /// </summary>
        public Packet()
        {
            Data = new ArraySegment<byte>();
        }

        public Packet(ArraySegment<byte> data)
        {
            Data = data;
        }

        /// <summary>
        /// 解析包
        /// </summary>
        /// <returns></returns>
        public abstract bool Deserialize();

        /// <summary>
        /// 解析包
        /// </summary>
        /// <param name="reader">含有包数据的reader<see cref="PackReader"/></param>
        /// <returns></returns>
        public abstract bool Deserialize(ref PackReader reader);

        /// <summary>
        /// 封包
        /// </summary>
        /// <returns></returns>
        public abstract bool Serialize();

        /// <summary>
        /// 分析包是否合法
        /// </summary>
        /// <param name="buffer">包数据</param>
        /// <returns></returns>
        public abstract bool Analysis(ArraySegment<byte> buffer);

        /// <summary>
        /// 获取包数据域
        /// </summary>
        /// <typeparam name="TPacketData">数据域数据结构<see cref="DeviceCommand"/></typeparam>
        /// <returns></returns>
        public abstract TPacketData GetData<TPacketData>() where TPacketData : IDeviceCommand;

        /// <summary>
        /// 获取多包结构的数据域
        /// </summary>
        /// <typeparam name="TPacketData">多包数据域数据结构<see cref="MultiPackCommand"/></typeparam>
        /// <returns></returns>
        public abstract TPacketData GetMultiPackData<TPacketData>() where TPacketData : MultiPackCommand;

        /// <summary>
        /// 创建数据包
        /// </summary>
        /// <typeparam name="TPacketData">数据域数据结构<see cref="DeviceCommand"/></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        internal abstract ArraySegment<byte> CreatePacket<TPacketData>(TPacketData data) where TPacketData : DeviceCommand;

        /// <summary>
        /// 创建多包结构的数据包（内容超过240字节会自动拆分)
        /// </summary>
        /// <typeparam name="TPacketData">多包数据域数据结构<see cref="MultiPackCommand"/></typeparam>
        /// <param name="data">数据域的数据</param>
        /// <returns></returns>
        internal abstract IEnumerable<ArraySegment<byte>> CreateMultiplePacket<TPacketData>(TPacketData data) where TPacketData : MultiPackCommand;
    }
}
