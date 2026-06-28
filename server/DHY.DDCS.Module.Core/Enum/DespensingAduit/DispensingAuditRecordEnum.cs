public enum DispensingAuditRecordEnum
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,
    /// <summary>
    /// 自动审核
    /// </summary>
    [Description("自动审核")]
    Auto = 1,
    /// <summary>
    /// 人工审核
    /// </summary>
    [Description("人工补配")]
    Replenish,
    /// <summary>
    /// 复核称重
    /// </summary>
    [Description("复核称重")]
    FinalCheckWeight,
    /// <summary>
    /// 终核
    /// </summary>
    [Description("终审")]
    FinalCheck
}
