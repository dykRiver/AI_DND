namespace DHY.DDCS.Module.Core.Consts;

/// <summary>
/// 通用常量
/// </summary>
public class ConfigConst
{
    /// <summary>
    /// 煎药加水量计算系数
    /// </summary>
    public const string DecoctionWater = "decoction_water_coefficient";

    /// <summary>
    /// 煎药加水量计算吸水比修正系数
    /// </summary>
    public const string DecoctionWaterCorrectionFactor = "decoction_water_coefficient_factor";

    /// <summary>
    /// 所有煎煮时间延时
    /// </summary>
    public const string DecoctionDelay = "decoction_delay";

    /// <summary>
    /// 群药固定煎煮时间配置
    /// </summary>
    public const string FixedDecoctionTime = "fixed_decoction_time";

    /// <summary>
    /// 群药固定泡药时间
    /// </summary>
    public const string FixedSoakDuration = "fixed_soak_duration";

    /// <summary>
    /// 开启煎煮沥液体积合格检测（会影响机器人搬运任务的顺序，提前释放煎药机)
    /// </summary>
    public const string DecoctionTargetVolumnCheck = "decoction_targetvolumn_check";
    /// <summary>
    /// 加水复煎偏离值(应为负数)
    /// </summary>

    public const string RefryDeviation = "refry_deviation";
    /// <summary>
    /// 浓缩偏离值
    /// </summary>
    public const string CondenseDeviation = "condense_deviation";

    /// <summary>
    /// 合并煎药时需排除的药品，逗号间隔
    /// </summary>
    public const string MergeExceptMedications = "merge_except_medications";

    /// <summary>
    /// 先煎药的最小煎煮时间
    /// </summary>
    public const string FryFirstMinTime = "fry_first_min_time";

    /// <summary>
    /// 参与计算加水量的重量单位
    /// </summary>
    public const string AcceptDrugUnits = "accept_drug_units";

    /// <summary>
    /// 转空净桶回流下发方向值
    /// </summary>
    public const string ContainerResetDirection = "container_reset_direction";

    /// <summary>
    /// 二煎空桶分配下发方向值
    /// </summary>
    public const string ContainerDispatchDirection = "container_dispatch_direction";

    /// <summary>
    /// 二煎药加水比例
    /// </summary>
    public const string SecondDecoctionWaterPercent = "second_decoction_water_percent";

    /// <summary>
    /// 后下药是否浸泡
    /// </summary>
    public const string DecoctLaterSoakEnable = "decoct_later_soak_enable";

    /// <summary>
    /// 先煎药最少加水量
    /// </summary>
    public const string DecoctFirstLeastWater = "decoct_first_least_water";

    /// <summary>
    /// 煎煮每分钟的蒸发量
    /// </summary>

    public const string DecoctEvaporationPerMinutes = "decoct_evaporation_per_minutes";

    /// <summary>
    /// 包装丢弃袋数
    /// </summary>
    public const string PackingDiscardedNum = "packing_discarded_num";

    /// <summary>
    /// 单巷道允许同时存在的最大拆方数量
    /// </summary>
    public const string MaxPartialPrescriptionPackerTask = "max_partial_prescription_packer_task";

    /// <summary>
    /// 开启绑桶前调剂复核状态检查
    /// </summary>
    public const string CheckStatusBeforeBindBucket = "check_status_before_bind_bucket";

    /// <summary>
    /// 设置绑桶前状态验证调剂30/复核40
    /// </summary>
    public const string CheckStatusBeforeBindBucketValue = "check_status_before_bind_bucket_value";

    /// <summary>
    /// 可接收的最小得液量的处方
    /// </summary>
    public const string AcceptMinTargetVolumPrescription = "accept_min_target_volum_prescription";

    /// <summary>
    /// 允许推送到自动化的医院;允许推送处方到新厂自动化煎药的医院ID，为空表示不限制医院; 形如“医院ID1|医院ID2”。医院ID见Hospital表的HID字段
    /// </summary>
    public const string CanPushHospitals = "can_push_hospitals";

    /// <summary>
    /// 不允许推送到自动化的特煎类型；形如“先煎|后下|另煎|另包”,为空则不限制；处方如果有指定的药品脚注，则不能推送到自动化
    /// </summary>
    public const string CannotSpecialDecoction = "cannot_special_decoction";

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

    /// <summary>
    /// 限制不能推到自动化的毒性药品，如果处方包含这些药品则不能推送到全自动；形如“附片|制川乌|制草乌|蛇六谷|雷公藤”，为空则不限制；
    /// </summary>
    public const string CannotPushToxicDrugs = "cannot_push_toxic_drugs";

    /// <summary>
    /// 能推送到自动化的最大贴数
    /// </summary>
    public const string CanPushMaxDoseLimit = "can_push_max_dose_limit";

    /// <summary>
    /// 能推送到自动化的处方最大药品重量
    /// </summary>
    public const string CanPushMaxWeightLimit = "can_push_max_weight_limit";

    /// <summary>
    /// 是否启用沥液缓存
    /// </summary>

    public const string LiftBufferEnable = "lift_buffer_enable";
    /// <summary>
    /// 是否经过加水工位
    /// </summary>
    public const string IsPassWaterStation = "is_pass_waterstation";

    /// <summary>
    /// 每巷道二煎空桶数
    /// </summary>
    public const string TwiceEmptyBufferCapacity = "twice_empty_buffer_capacity";

    /// <summary>
    /// 包装机与打印机Ip关联配置
    /// </summary>
    public const string PackingPrinter = "packing_printer";
}
