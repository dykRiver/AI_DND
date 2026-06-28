namespace DHY.DDCS.Module.Core;

/// <summary>
/// 桶事件类型，1到达、2离开
/// </summary>
public enum ContainerEventEnum : ushort
{
    /// <summary>
    /// 到达
    /// </summary>
    Reach = 1,
    /// <summary>
    /// 离开
    /// </summary>
    Leave = 2,
}
