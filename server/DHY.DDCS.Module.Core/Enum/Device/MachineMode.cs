namespace DHY.DDCS.Module.Core.Enum.Device
{
    public enum MachineMode
    {
        [Description("停用")]
        INVALID=0,
        [Description("生产")]
        PRODUCTION=1,
        [Description("维护")]
        MAINTENANCE=2,
        [Description("手动")]
        MANUAL=3
    }
}
