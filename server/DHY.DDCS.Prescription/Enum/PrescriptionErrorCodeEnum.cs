using Furion.FriendlyException;

/// <summary>
/// 处方模块错误代码
/// 起始错误号:1000-1999
/// </summary>
[ErrorCodeType]
public enum PrescriptionErrorCodeEnum
{
    /// <summary>
    /// 重复的处方
    /// </summary>
    [ErrorCodeItemMetadata("重复的处方号{0}")]
    P1000,
    /// <summary>
    /// 重复的处方
    /// </summary>
    [ErrorCodeItemMetadata("重复的处方号{0}，索引号:{1}")]
    P1001,
    /// <summary>
    /// 指定ID的处方不存在
    /// </summary>
    [ErrorCodeItemMetadata("指定ID的处方[{0}]不存在")]
    P1002,
    /// <summary>
    /// 无效操作
    /// </summary>
    [ErrorCodeItemMetadata("无效操作：{0}")]
    P1003,
    /// <summary>
    /// 数据存储失败
    /// </summary>
    [ErrorCodeItemMetadata("{0} 数据存储失败")]
    P1004,
    /// <summary>
    /// 拆方信息不存在
    /// </summary>
    [ErrorCodeItemMetadata("无效的绑桶操作，拆方任务 {0} 不存在")]
    P1005,
    /// <summary>
    /// 不能推送处方
    /// </summary>
    [ErrorCodeItemMetadata("处方正在进行{0}任务，不允许推送")]
    P1006,
    /// <summary>
    /// 绑桶失败
    /// </summary>
    [ErrorCodeItemMetadata("绑桶失败，任务号：{0},桶号:{1}")]
    P1007,
    /// <summary>
    /// 该处方没有药品信息
    /// </summary>
    [ErrorCodeItemMetadata("处方【{0}】没有药品信息")]
    P1008,
    /// <summary>
    /// 处方已推送到调剂系统，正在调剂队列，不允许重复推送
    /// </summary>
    [ErrorCodeItemMetadata("处方{0}已推送到调剂系统，正在调剂队列，不允许重复推送")]
    P1009,
    /// <summary>
    /// 不支持的处方类型
    /// </summary>
    [ErrorCodeItemMetadata("不支持的处方类型，请在煎药系统检查药品脚注")]
    P1010,
    /// <summary>
    /// 需要拆方
    /// </summary>
    [ErrorCodeItemMetadata("系统未开启拆方功能，当前处方需要拆方才能生产")]
    P1011,
    /// <summary>
    /// 处方药液量太少
    /// </summary>
    [ErrorCodeItemMetadata("该处方目标得液量少于{0}L，不适用全自动生产")]
    P1012,
    /// <summary>
    /// 此医院的处方不允许推送到全自动化
    /// </summary>
    [ErrorCodeItemMetadata("此医院的处方（医院ID:{0}）不允许推送到全自动化")]
    P1013,
    /// <summary>
    /// 处方不允许的特煎类型
    /// </summary>
    [ErrorCodeItemMetadata("不接收处方药品中包含{0}的处方；{1}是{0}!\"")]
    P1014,
    /// <summary>
    /// 不允许的服用方式
    /// </summary>
    [ErrorCodeItemMetadata("不接收服用方式是{0}的处方!\"")]
    P1015,
    /// <summary>
    /// 不允许的加工类型
    /// </summary>
    [ErrorCodeItemMetadata("不接收加工类型是{0}的处方!\"")]
    P1016,
    /// <summary>
    /// 不允许的代煎代配
    /// </summary>
    [ErrorCodeItemMetadata("不接收{0}处方!\"")]
    P1017,
    /// <summary>
    /// 允许的包装量
    /// </summary>
    [ErrorCodeItemMetadata("仅接收包装量{0}ml的处方!\"")]
    P1018,
    /// <summary>
    /// 不允许的药品；如果处方有此药品则不能推送
    /// </summary>
    [ErrorCodeItemMetadata("不接收包含毒性药品{0}的处方!\"")]
    P1019,
    /// <summary>
    /// 最大贴数限制
    /// </summary>
    [ErrorCodeItemMetadata("不接收贴数大于{0}的处方!\"")]
    P1020,
    /// <summary>
    /// 处方最大药品重量限制
    /// </summary>
    [ErrorCodeItemMetadata("不接收处方重量大于{0}g的处方!\"")]
    P1021

    /*


/// <summary>
/// 允许能推送到自动化的服用方式（如内服、口服）；形如“内服|煎服”,为空则不限制；数字是表sysKeyValue对应服用方式的值
/// </summary>
public const string CanPushTakeMethods = "can_push_take_methods";

/// <summary>
/// 允许能推送到自动化的加工类型（如汤药）；形如“1|3”，为空则不限制；数字是表sysKeyValue对应加工类型的值
/// </summary>
public const string CanPushProcessTypes = "can_push_process_types";

/// <summary>
/// 允许能推送到自动化的代煎代配（如代煎1、代配0）；形如“1|0”，为空则不限制；
/// </summary>
public const string CanPushIsDaijianTypes = "can_push_is_daijian_types";

/// <summary>
/// 允许能推送到自动化的包装量范围；如“50~250”表示包装量范围[50,250]，为空表示不限制；默认50~250；
/// </summary>
public const string CanPushPackageNum = "can_push_packageNum";
    */
}