/// <summary>
/// 模块错误代码
/// 起始错误号:4000-4999
/// </summary>
[ErrorCodeType]
public enum ErrorCodeEnum
{
    /// <summary>
    /// 该项已存在
    /// </summary>
    [ErrorCodeItemMetadata("该项{0}已存在")]
    S4000,
    /// <summary>
    /// 指定ID的记录不存在
    /// </summary>
    [ErrorCodeItemMetadata("指定ID的记录[{0}]不存在")]
    S4001,
    /// <summary>
    /// 无效操作
    /// </summary>
    [ErrorCodeItemMetadata("无效操作：{0}")]
    S4002,
}