namespace DHY.DDCS.Module.Core.Enum;

/// <summary>
/// 容器运行状态
/// </summary>
public enum ContainerRunningModeEnum
{
    UnKnown,
    /// <summary>
    /// 启动发桶模式
    /// </summary>
    Kickoff,
    /// <summary>
    /// 自检模式
    /// </summary>
    SelfChecking,
    /// <summary>
    /// 生产模式
    /// </summary>
    Production,
    /// <summary>
    /// 结束收桶模式
    /// </summary>
    Knockoff
}
