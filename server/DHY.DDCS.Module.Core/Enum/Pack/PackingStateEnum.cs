namespace DHY.DDCS.Module.Core.Enum.Pack
{
    /// <summary>
    /// 工作状态
    /// </summary>
    public enum PackingStateEnum
    {
        [Description("未知")]
        Unknow = 0,
        [Description("空闲")]
        Idle = 1,
        [Description("开始工作")]
        Start = 2,
        [Description("工作中")]
        Excute = 3,
        [Description("工作完成")]
        Completed = 4
    }
}
