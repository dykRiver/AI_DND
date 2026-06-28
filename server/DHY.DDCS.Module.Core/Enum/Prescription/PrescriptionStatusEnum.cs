public enum PrescriptionStatusEnum
{

    /// <summary>
    /// 已推送并处于锁定状态，调剂前不允许重复推送
    /// </summary>
    [Description("已推送")]
    Locked = -1,

    /// <summary>
    /// 待分配
    /// </summary>
    [Description("待分配")]
    Ready,

    /// <summary>
    /// 发放容器
    /// </summary>
    [Description("发放容器")]
    [TaskTableName("DispensingTask")]
    SentContainer,

    /// <summary>
    /// 绑定容器
    /// </summary>
    [Description("绑定容器")]
    [TaskTableName("DispensingTask")]
    BindContainer,

    /// <summary>
    /// 自动调剂
    /// </summary>
    [Description("自动调剂")]
    [TaskTableName("DispensingTask")]
    Dispensing,

    /// <summary>
    /// 人工补配
    /// </summary>
    [Description("人工补配")]
    [TaskTableName("ReplenishTask")]
    Replenish,

    /// <summary>
    /// 复核
    /// </summary>
    [Description("复核")]
    [TaskTableName("RecheckTask")]
    Recheck,

    /// <summary>
    /// 加水
    /// </summary>
    [Description("加水")]
    [TaskTableName("FillWaterTask")]
    FillWater,

    /// <summary>
    /// 浸泡
    /// </summary>
    [Description("浸泡")]
    [TaskTableName("SoakTask")]
    Soak,

    /// <summary>
    /// 煎煮
    /// </summary>
    [Description("煎煮")]
    [TaskTableName("DecoctionTask")]
    Decoction,

    /// <summary>
    /// 包装
    /// </summary>
    [Description("包装")]
    [TaskTableName("PackingTask")]
    Packing,

    /// <summary>
    /// 完成
    /// </summary>
    [Description("完成")]
    Completed
}

public enum PrescriptionManageStatusEnum
{
    未知 = 0,
    未开始 = 1,
    已完成药嘱 = 2,
    接方 = 10,
    审核 = 20,
    调剂 = 30,
    复核 = 40,
    泡药 = 50,
    煎药 = 60,
    包装 = 70,
    成品复核 = 75,
    发货 = 80,
}