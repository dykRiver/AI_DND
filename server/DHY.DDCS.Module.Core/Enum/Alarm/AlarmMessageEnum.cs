namespace DHY.DDCS.Module.Core.Enum.Alarm;

/// <summary>
/// 报警消息类型
/// </summary>
[Description("报警消息码")]
public enum AlarmMessageEnum
{
    [Description("未知错误")]
    C0000 = 0,
    /// <summary>
    /// 包装机开盖错误
    /// </summary>
    [Description("包装机开盖错误")]
    PackerCoverOpenError=1,
    /// <summary>
    /// 包装机关盖错误
    /// </summary>
    [Description("包装机关盖错误")]
    PackerCoverCloseError,
    /// <summary>
    /// 包装机标签错误
    /// </summary>
    [Description("包装机标签错误")]
    PackerLabelError,
    /// <summary>
    /// 包装机可以包装错误
    /// </summary>
    [Description("包装机可以包装错误")]
    PackerCanPackingError,
    /// <summary>
    /// 包装机走卷错误
    /// </summary>
    [Description("包装机走卷错误")]
    PackerRollError,
    /// <summary>
    /// 包装机清洗错误
    /// </summary>
    [Description("包装机清洗错误")]
    PackerCleanError,
    /// <summary>
    /// 包装机开盖完成错误
    /// </summary>
    [Description("包装机开盖完成错误")]
    PackerCoverOpenCompleteError,
    /// <summary>
    /// 包装机关盖完成错误
    /// </summary>
    [Description("包装机关盖完成错误")]
    PackerCoverCloseCompleteError,
    /// <summary>
    /// 包装机开始包装错误
    /// </summary>
    [Description("包装机开始包装错误")]
    PackerStartPackingError,
    /// <summary>
    /// 包装机包装完成错误
    /// </summary>
    [Description("包装机包装完成错误")]
    PackerPackingCompleteError,
    /// <summary>
    /// 包装机清洗完成错误
    /// </summary>
    [Description("包装机清洗完成错误")]
    PackerCleanCompleteError,
    /// <summary>
    /// 包装机状态错误
    /// </summary>
    [Description("包装机状态错误")]
    PackerInfoError,
}
