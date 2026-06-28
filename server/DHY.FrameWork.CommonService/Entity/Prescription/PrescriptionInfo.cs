using System.Text.Json.Serialization;

namespace DHY.DDCS.Module.Common.Entity;

/// <summary>
/// 原始处方
/// 处方结构：
/// </summary>
[SugarTable(null, "DDCS原始处方")]
public class PrescriptionInfo : EntityTenant
{
    /// <summary>
    /// 外部处方号
    /// </summary>
    [SugarColumn(ColumnDescription = "外部处方号", Length = 100)]
    [Required, MaxLength(100)]
    public string PrescriptionNo { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "患者姓名", Length = 100)]
    [Required, MaxLength(100)]
    public string PatientName { get; set; }

    /// <summary>
    /// 处方状态
    /// </summary>
    [SugarColumn(ColumnDescription = "管理系统处方状态")]
    [Required]
    public PrescriptionManageStatusEnum State { get; set; }

    /// <summary>
    /// 贴数/剂数
    /// </summary>
    [SugarColumn(ColumnDescription = "贴数/剂数")]
    [Required]
    public int Dosage { get; set; }

    /// <summary>
    /// 服用次数
    /// </summary>
    [SugarColumn(ColumnDescription = "次数")]
    [Required]
    public int Frequency { get; set; }

    /// <summary>
    /// 服用方式
    /// </summary>
    [SugarColumn(ColumnDescription = "服用方式")]
    [Required]
    public int Usage { get; set; }

    /// <summary>
    /// 煎药方案
    /// </summary>
    [SugarColumn(ColumnDescription = "煎药方案")]
    [Required]
    public int DecoctionScheme { get; set; }

    /// <summary>
    /// 群药加水量
    /// </summary>
    [SugarColumn(ColumnDescription = "群药加水量", IsNullable = true)]
    public int? GroupWater { get; set; }

    /// <summary>
    /// 群药泡药时间；单位：分钟
    /// </summary>
    [SugarColumn(ColumnDescription = "群药泡药时间", IsNullable = true)]
    public int? GroupSoakWaterTime { get; set; }

    /// <summary>
    /// 群药一煎时间；单位：分钟
    /// </summary>
    [SugarColumn(ColumnDescription = "群药一煎时间", IsNullable = true)]
    public int? GroupFirstDecoctionTime { get; set; }

    /// <summary>
    /// 群药二煎时间；单位：分钟
    /// </summary>
    [SugarColumn(ColumnDescription = "群药二煎时间", IsNullable = true)]
    public int? GroupSecondDecoctionTime { get; set; }

    /// <summary>
    /// 包装量
    /// </summary>
    [SugarColumn(ColumnDescription = "包装量")]
    [Required]
    public int PackageNum { get; set; }

    /// <summary>
    /// 原始处方JSON，包含患者信息和药品信息
    /// </summary>
    [SugarColumn(ColumnDescription = "原始处方", ColumnDataType = "text")]
    [JsonIgnore]
    [Required]
    public string PrescriptionJson { get; set; }

    /// <summary>
    /// 医院Id
    /// </summary>
    [SugarColumn(ColumnDescription = "医院Id", DefaultValue = "0")]
    [Required]
    public long HospitalId { get; set; }

    /// <summary>
    /// 配送方式Id
    /// </summary>
    [SugarColumn(ColumnDescription = "配送方式Id", DefaultValue = "0")]
    public byte? DeliveryMethodId { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    [SugarColumn(ColumnDescription = "优先级", IsNullable = true)]
    public PriorityEnum Priority { get; set; }

    /// <summary>
    /// 作废标志，1作废，0不作废
    /// </summary>
    [SugarColumn(ColumnDescription = "作废标志", DefaultValue = "0")]
    [Required]
    public bool Cancellation { get; set; }

    /// <summary>
    /// 处方明细
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    [Navigate(NavigateType.OneToMany, nameof(PrescriptionDetail.Pid))]
    public List<PrescriptionDetail> Details { get; set; }

    /// <summary>
    /// 拆方列表
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    [Navigate(typeof(DDCSPrescriptionMapping), nameof(DDCSPrescriptionMapping.Pid), nameof(DDCSPrescriptionMapping.DDCSPid))]
    public List<DDCSPrescription> DDCSPrescriptions { get; set; }

    /// <summary>
    /// 执行系统-处方完成状态
    /// </summary>
    [SugarColumn(ColumnDescription = "执行系统-处方状态")]
    [Required]
    public PrescriptionStatusEnum DDCSStatus { get; set; }

    /// <summary>
    ///  是否是二煎处方，管理系统推送来时把一煎二煎时间都放在群药一煎二煎时间字段上了
    /// </summary>
    [SugarColumn(IsIgnore = true)]
    public bool HasTwiceDecoction => GroupSecondDecoctionTime.HasValue && GroupSecondDecoctionTime.Value > 0;

    /// <summary>
    /// 性别
    /// </summary>
    [SugarColumn(ColumnDescription = "性别", IsNullable =true)]
    public int? Sex { get; set; }

    /// <summary>
    /// 年龄
    /// </summary>
    [SugarColumn(ColumnDescription = "年龄", IsNullable = true)]
    public int?   Age { get; set; }

    /// <summary>
    /// 诊断
    /// </summary>
    //[SugarColumn(ColumnDescription = "诊断", IsNullable = true)]
    //public string Remark { get; set; }
}