/// <summary>
/// 机器类型
/// </summary>
public enum DeviceTypeEnum
{
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// 煎药机
    /// </summary>
    [Description("煎药机")]
    Decoctor = 1,

    /// <summary>
    /// 包装机
    /// </summary>
    [Description("包装机")]
    Packer = 2,

    /// <summary>
    /// 调剂设备
    /// </summary>
    [Description("调剂设备")]
    Dispensor = 3,
}

