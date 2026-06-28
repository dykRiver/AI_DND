using DHY.FrameWork.Common;

/// <summary>
/// 通信交互点类型
/// </summary>
public enum StationTypeEnum
{
    /// <summary>
    /// 未知
    /// </summary>
    [Description("未知")]
    Unknown = 0,

    /// <summary>
    /// 绑桶工位
    /// </summary>
    [Description("绑桶工位")]
    BindStation = 5,

    /// <summary>
    /// 加水工位
    /// </summary>
    [Description("加水工位")]
    WaterStation = 10,

    /// <summary>
    /// 巷道岔道口移栽工位
    /// </summary>
    [Description("巷道岔道口移栽工位")]
    RoadwayCrossStation = 20,

    /// <summary>
    /// RGV取桶工位
    /// </summary>
    [Description("RGV取桶工位")]
    RgvPullStation = 30,

    /// <summary>
    /// RGV工位
    /// </summary>
    [Description("RGV工位")]
    RgvStation = 40,

    /// <summary>
    /// 提撑挤压工位
    /// </summary>
    [Description("提撑挤压工位")]
    LiftStation = 50,
    /// <summary>
    /// 提撑挤压缓存区工位
    /// </summary>
    [Description("提撑挤压缓存区工位")]
    LiftBufferStation=51,

    /// <summary>
    /// 药桶缓存区工位
    /// </summary>
    [Description("药桶缓存区工位")]
    MedicineContainerBufferStation = 60,

    /// <summary>
    /// 二煎储液桶缓存区工位
    /// </summary>
    [Description("二煎储液桶缓存区工位")]
    StorageContainerBufferStation = 61,

    /// <summary>
    /// 先煎后下二煎固定、翻倒工位
    /// </summary>
    [Description("煎煮翻倒总工位")]
    DecoctionOverTurnGeneralStation = 70,

    /// <summary>
    /// 先煎后下二煎固定工位
    /// </summary>
    [Description("煎煮翻倒固定工位")]
    DecoctionFixStation = 71,

    /// <summary>
    /// 先煎后下二煎翻倒工位
    /// </summary>
    [Description("煎煮翻倒工位")]
    DecoctionOverTurnStation = 72,

    /// <summary>
    /// 包装翻倒工位
    /// </summary>
    [Description("包装翻倒工位")]
    PackOverturnStation = 80,

    /// <summary>
    /// 组桶工位
    /// </summary>
    [Description("组桶工位")]
    CombineContainerStation = 90,

    /// <summary>
    /// 调度空净桶工位
    /// </summary>
    [Description("调度空净桶工位")]
    DispatchStation = 100,

    /// <summary>
    /// 清洗区异常桶排出工位
    /// </summary>
    [Description("清洗区异常桶排出工位")]
    CleanExceptionStation = 110,

    /// <summary>
    /// 清洗区分配和转空净桶工位
    /// </summary>
    [Description("清洗区分配和转空净桶工位")]
    CleanDistributeStation = 120,

    /// <summary>
    /// 清洗前工位
    /// </summary>
    [Description("清洗前工位")]
    CleanBeforeStation = 130,
    /// <summary>
    /// 煎煮工位
    /// </summary>
    [Description("煎煮工位")]
    DecoctorStation = 131,

    /// <summary>
    /// 调剂桶调度工位
    /// </summary>
    [Description("调剂桶调度工位")]
    DispensingDispatchStation = 140,

    /// <summary>
    /// 调剂系统
    /// </summary>
    [Description("调剂系统")]
    [InteractiveGroup(InteractiveGroupEnum.BusinessSystem)]
    DispensingSystem = 141,
    /// <summary>
    /// 调剂称重工位
    /// </summary>
    [Description("调剂称重工位")]
    DispensingWeightStation = 150,

    /// <summary>
    /// 调剂落药口工位
    /// </summary>
    [Description("调剂落药口工位")]
    DispensingFallDrugStation = 160,

    /// <summary>
    /// 调剂初审工位
    /// </summary>
    [Description("调剂初审工位")]
    DispensingAuditFirstStation = 170,

    /// <summary>
    /// 调剂补配前分配工位
    /// </summary>
    [Description("调剂补配前分配工位")]
    DispensingSupplementBeforeStation = 180,

    /// <summary>
    /// 调剂补配岔道口工位
    /// </summary>
    [Description("调剂补配岔道口工位")]
    DispensingSupplementRoadcrossStation = 190,

    /// <summary>
    /// 调剂补配缓冲区工位
    /// </summary>
    [Description("调剂补配缓冲区工位")]
    DispensingSupplementBufferStation = 191,

    /// <summary>
    /// 调剂补配工位
    /// </summary>
    [Description("调剂补配工位")]
    DispensingSupplementStation = 200,

    /// <summary>
    /// 调剂复核前称重工位
    /// </summary>
    [Description("调剂复核前称重工位")]
    DispensingCheckWeightStation = 210,

    /// <summary>
    /// 调剂复核工位
    /// </summary>
    [Description("调剂复核工位")]
    DispensingCheckStation = 220,

    /// <summary>
    /// 煎药机
    /// </summary>
    [Description("煎药机")]
    [InteractiveGroup(InteractiveGroupEnum.Device)]
    DecoctingMachine = 230,

    /// <summary>
    /// 包装机
    /// </summary>
    [Description("包装机")]
    [InteractiveGroup(InteractiveGroupEnum.Device)]
    PackagingMachine = 240,

    /// <summary>
    /// 清洗风干机
    /// </summary>
    [Description("清洗风干机")]
    CleanWasher = 250,

    /// <summary>
    /// 除渣刷锅机
    /// </summary>
    [Description("除渣刷锅机")]
    SlagRemover = 260,

    /// <summary>
    /// 挤压滤液机
    /// </summary>
    [Description("挤压滤液机")]
    ExtrusionFiltrate = 270,

    /// <summary>
    /// 二煎后下
    /// </summary>
    [Description("二煎后下")]
    SecondDecoction = 280,


    /// <summary>
    /// 提升机
    /// </summary>
    [Description("提升机")]
    Hoister = 290,

    /// <summary>
    /// 机器人工作站
    /// </summary>
    [Description("机器人工作站")]
    RobotWorkstation = 300,
    /// <summary>
    /// 加水机
    /// </summary>
    [Description("加水机")]
    WaterFiller = 310,

    /// <summary>
    /// 调剂线体
    /// </summary>
    [Description("调剂线体")]
    [InteractiveGroup(InteractiveGroupEnum.ProcessLine)]
    DispensingProductionLine = 500,

    /// <summary>
    /// 煎煮线体
    /// </summary>
    [Description("煎煮线体")]
    DecoctionProductionLine = 510,

    /// <summary>
    /// 煎煮巷道
    /// </summary>
    [Description("煎煮巷道")]
    DecoctionRoadway = 520,

    /// <summary>
    /// 总控系统
    /// </summary>
    [Description("总控系统")]
    MasterControlSystem = 530,

    /// <summary>
    /// 输送线体-挡停位
    /// </summary>
    [Description("输送线体-挡停")]
    ConveyorLine = 320,

    /// <summary>
    /// 输送线体-移栽机
    /// </summary>
    [Description("输送线体-移栽机")]
    LiftingShifting = 330,
    /// <summary>
    /// 存桶区
    /// </summary>
    [Description("存桶区")]
    ContainerInventory = 340,
}