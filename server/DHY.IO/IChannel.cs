using DHY.Core.Drivers;
using DHY.Core.Interfaces;

namespace DHY.IO
{
    /// <summary>
    /// 定义一个通信通道
    /// </summary>
    public interface IChannel : IControlAble, ISetupAble
    {
        /// <summary>
        /// 通道驱动
        /// </summary>
        IDriver Driver { get; }
    }
}
