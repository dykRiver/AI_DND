namespace DHY.DDCS.Module.Core.Enum.Decoction
{
    /// <summary>
    /// 煎药机开关盖状态枚举
    /// </summary>
    public enum CoverStateEnum
    {
        [Description("未知")]
        Unknow=0,
        [Description("开盖中")]
        Opening =1,
        [Description("已开盖")]
        Opened = 2,
        [Description("关盖中")]
        Closing = 3,
        [Description("已关盖")]
        Closed = 4,
    }
}
