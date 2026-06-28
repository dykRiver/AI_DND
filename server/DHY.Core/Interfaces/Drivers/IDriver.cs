using DHY.Core.Interfaces;
using Furion.ConfigurableOptions;

namespace DHY.Core.Drivers
{
    /// <summary>
    /// 设备适配器接口定义
    /// </summary>
    public interface IDriver : IControlAble, ISetupAble
    {
        /// <summary>
        /// 驱动名称
        /// </summary>
        string Name { get; init; }

        /// <summary>
        /// 驱动所使用协议
        /// </summary>
        IProtocol Protocol { get; }
        IConfigurableOptions GetOptions();
    }
}
