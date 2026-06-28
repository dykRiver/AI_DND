/// <summary>
/// 车辆任务状态枚举
/// 与VehicleRunningTaskStatusEnum的区别：
/// 1）两者关注的点有区别
/// 2）VehicleTaskStatusEnum关注的任务的整个业务周期，任务处于哪个阶段，如任务已分配、取货完成
/// 3）VehicleRunningTaskStatusEnum关注的是任务正在进行时的细节状态，包括暂停、继续的状态
/// </summary>
public enum VehicleTaskStatusEnum
{
    /// <summary>
    /// 待分配；也表示任务已创建；待分配指的是任务还没有分配给车辆（RGV）
    /// </summary>
    [Description("待分配")]
    Idle = 1,

    /// <summary>
    /// 已分配；任务分配给车辆（RGV），并不是通知给车辆
    /// </summary>
    [Description("已分配")]
    Assigned = 2,
    /// <summary>
    /// 已下发；已经将命令通知给车辆（RGV）
    /// </summary>
    [Description("已下发")]
    Sent = 3,
    /// <summary>
    /// 取货完成
    /// </summary>
    [Description("取货完成")]
    LoadingCompleted = 4,
    /// <summary>
    /// 卸货完成
    /// </summary>
    [Description("卸货完成")]
    UnloadingCompleted = 5,
    /// <summary>
    /// 完成
    /// </summary>
    [Description("完成")]
    Completed = 6,
    /// <summary>
    /// 作废
    /// </summary>
    [Description("作废")]
    Cancel = 7
}