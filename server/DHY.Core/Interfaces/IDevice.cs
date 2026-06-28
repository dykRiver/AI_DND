using DHY.Core.Drivers;
using DHY.Core.Interfaces;

/// <summary>
/// 表示设备接口
/// </summary>
public interface IDevice : IControlAble, ISetupAble
{
    /// <summary>
    /// 工位类型名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 设备名
    /// </summary>
    string DeviceName { get; set; }

    /// <summary>
    /// 设备工位号
    /// </summary>
    ushort DeviceNo { get; set; }

    /// <summary>
    /// 设备驱动
    /// </summary>
    IDriver Driver { get; set; }
}