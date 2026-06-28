public enum TaskStatuEnum
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,
    /// <summary>
    /// 空闲
    /// </summary>
    [Description("空闲")]
    ReadyToStart,
    /// <summary>
    /// 开始
    /// </summary>
    [Description("开始")]
    Started,
    /// <summary>
    /// 暂停
    /// </summary>
    [Description("暂停")]
    Paused,
    /// <summary>
    /// 停止
    /// </summary>
    [Description("停止")]
    Stoped,
    /// <summary>
    /// 完成
    /// </summary>
    [Description("完成")]
    Completed,
    /// <summary>
    /// 挂起
    /// </summary>
    [Description("挂起")]
    Suspending,
}