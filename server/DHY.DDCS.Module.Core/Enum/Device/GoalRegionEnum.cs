namespace DHY.DDCS.Module.Common.Enum.Device;

/// <summary>
/// 目标区域
/// </summary>
public enum GoalRegionEnum : byte
{
    /// <summary>
    /// 回流区
    /// </summary>
    [Description("回流区")]
    Backflow = 0,
    /// <summary>
    /// 清洗区
    /// </summary>
    [Description("清洗区")]
    Clean = 1,
    /// <summary>
    /// 异常桶排出区
    /// </summary>
    [Description("异常桶排出区")]
    Abnormal = 2
}
