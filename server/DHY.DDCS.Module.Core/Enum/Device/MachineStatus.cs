namespace DHY.DDCS.Module.Core.Enum.Device
{
    public enum MachineStatus
    {
        [Description("未定义")]
        UNDEFINED = 0,
        [Description("清除中")]
        CLEARING = 1,
        [Description("已停止")]
        STOPPED = 2,
        [Description("启动中")]
        STARTING=3,
        [Description("初始完成")]
        IDLE=4,
        [Description("已外部暂停")]
        SUSPENDED=5,
        [Description("生产中")]
        EXECUTE=6,
        [Description("停止中")]
        STOPPING=7,
        [Description("退出中")]
        ABORTING=8,
        [Description("已退出")]
        ABORTED=9,
        [Description("内部暂停中")]
        HOLDING=10,
        [Description("已内部暂停")]
        HELD=11,
        [Description("取消内部暂停")]
        UNHOLDING=12,
        [Description("外部暂停中")]
        SUSPENDING=13,
        [Description("取消外部暂停")]
        UNSUSPENDING=14,
        [Description("复位中")]
        RESETTING=15,
        [Description("完成中")]
        COMPLETING=16,
        [Description("已完成")]
        COMPLETED=17,
    }
}
