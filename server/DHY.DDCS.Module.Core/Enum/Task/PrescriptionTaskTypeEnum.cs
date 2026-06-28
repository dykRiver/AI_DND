public enum PrescriptionTaskTypeEnum
{
    /// <summary>
    /// 自检(绑桶用)，查询时按常规流程枚举查
    /// </summary>
    SelfCheck,
    /// <summary>
    /// 自动调剂
    /// </summary>
    Dispensing,

    /// <summary>
    /// 人工补配
    /// </summary>
    Replenish,

    /// <summary>
    /// 复核
    /// </summary>
    Recheck,

    /// <summary>
    /// 加水
    /// </summary>
    FillWater,

    /// <summary>
    /// 浸泡
    /// </summary>
    Soak,

    /// <summary>
    /// 煎煮
    /// </summary>
    Decoction,

    /// <summary>
    /// 包装
    /// </summary>
    Packing,
}