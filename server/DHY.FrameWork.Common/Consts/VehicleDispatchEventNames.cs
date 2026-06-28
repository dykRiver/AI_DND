/// <summary>
/// 用于定义若干关于车辆调度的事件。如RGV调度。
/// </summary>
public class VehicleEventNames
{
    ///// <summary>
    ///// 车辆调度事件；用于触发车辆（如RGV）调度运行。其中{0}是工位类型枚举DHY.Core.Enums.DeviceTypeEnum,{1}是车辆设备号（如RGV的设备号）
    ///// </summary>
    //public const string VehicleDispatchEvent = "VehicleDispatchEvent{0}{1}";

    ///// <summary>
    ///// 车辆任务事件；用于触发(间接触发)分配给车辆（如RGV）任务。其中{0}是工位类型枚举DHY.Core.Enums.DeviceTypeEnum
    ///// </summary>
    //public const string VehicleTaskEvent = "VehicleTaskEvent{0}";

    /// <summary>
    /// 车辆任务事件；用于触发(间接触发)分配给车辆（如RGV）任务。其中{0}是工位类型枚举DHY.Core.Enums.DeviceTypeEnum
    /// </summary>
    public const string VehicleTaskEvent = "VehicleTaskEvent{0}";

    /// <summary>
    /// 创建车辆任务事件;其中{0}是工位类型枚举DHY.Core.Enums.DeviceTypeEnum,{1}是车辆设备号（如RGV的设备号）
    /// </summary>
    public const string CreateVehicleTaskEvent = "CreateVehicleTaskEvent{0}{1}";

    /// <summary>
    /// 更新车辆任务状态事件;其中{0}是工位类型枚举DHY.Core.Enums.DeviceTypeEnum,{1}是车辆设备号（如RGV的设备号）
    /// </summary>
    public const string UpdateVehicleTaskStatusEvent = "UpdateVehicleTaskStatusEvent{0}{1}";

    /// <summary>
    /// 运行一次车辆任务调度模块事件;其中{0}是工位类型枚举DHY.Core.Enums.DeviceTypeEnum,{1}是车辆设备号（如RGV的设备号）
    /// 注意：不是启动车辆任务调度模块，而是触发车辆任务调度模块运行一次
    /// </summary>
    public const string RunVehicleDipatcherOnceEvent = "RunVehicleDipatcherOnceEvent{0}{1}";

    /// <summary>
    /// 
    /// </summary>
    public const string DispatcherManagerTaskf = "DispatcherManagerTaskf{0}{1}";


    /// <summary>
    /// 释放车辆事件;其中{0}是工位类型枚举DHY.Core.Enums.DeviceTypeEnum,{1}是车辆设备号（如RGV的设备号）
    /// </summary>
    public const string ReleaseVehicleEvent = "ReleaseVehicleEvent{0}{1}";
}