using System.ComponentModel.DataAnnotations;

public class PrescriptionInfoOutput
{
    /// <summary>
    /// 原始处方id -->（pid）
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// 外部处方号
    /// </summary>
    [Required, MaxLength(100)]
    public string PrescriptionNo { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    [Required, MaxLength(100)]
    public string PatientName { get; set; }

    /// <summary>
    /// 处方状态
    /// </summary>
    [Required]
    public PrescriptionManageStatusEnum State { get; set; }

    /// <summary>
    /// 贴数/剂数
    /// </summary>
    [Required]
    public int Dosage { get; set; }

    /// <summary>
    /// 服用次数
    /// </summary>
    [Required]
    public int Frequency { get; set; }

    /// <summary>
    /// 服用方式
    /// </summary>
    [Required]
    public int Usage { get; set; }

    /// <summary>
    /// 煎药方案
    /// </summary>
    [Required]
    public int DecoctionScheme { get; set; }

    /// <summary>
    /// 群药加水量
    /// </summary>
    public int? GroupWater { get; set; }

    /// <summary>
    /// 包装量
    /// </summary>
    [Required]
    public int PackageNum { get; set; }

    /// <summary>
    /// 群药泡药时间；单位：分钟
    /// </summary>
    public int GroupSoakWaterTime { get; set; }

    /// <summary>
    /// 群药一煎时间；单位：分钟
    /// </summary>
    public int? GroupFirstDecoctionTime { get; set; }

    /// <summary>
    /// 群药二煎时间；单位：分钟
    /// </summary>
    public int? GroupSecondDecoctionTime { get; set; }

    /// <summary>
    /// 原始处方JSON，包含患者信息和药品信息
    /// </summary>
    [Required]
    public string PrescriptionJson { get; set; }

    /// <summary>
    /// 作废标志，1作废，0不作废
    /// </summary>
    [Required]
    public bool Cancellation { get; set; }

    /// <summary>
    /// 医院Id
    /// </summary>
    public long HospitalId { get; set; }

    /// <summary>
    /// 配送方式
    /// </summary>
    public string DeliveryMethod { get; set; }

    /// <summary>
    /// 桶号
    /// </summary>
    public string ContainerNos { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public PriorityEnum Priority { get; set; }

    /// <summary>
    /// 处方明细
    /// </summary>
    public List<PrescriptionInfoDetailOutput> Details { get; set; }

    /// <summary>
    /// 拆方列表
    /// </summary>
    public List<DDCSPrescriptionOutput> DDCSPrescriptions { get; set; }

    /// <summary>
    /// 服用方式
    /// </summary>
    public string TakeMethod { get; set; }

    /// <summary>
    /// 煎药方案
    /// </summary>
    public string Decscheme { get; set; }

    /// <summary>
    /// 拆方加水量
    /// </summary>
    public int? WaterAmount { get; set; }

    /// <summary>
    /// 目标得液量
    /// </summary>
    public int? TargetVolumn { get; set; }

    /// <summary>
    /// 得液重量-》用作实际得液量
    /// </summary>
    public ushort? BrothWeight { get; set; }

    /// <summary>
    /// 桶号
    /// </summary>
    public long? ContainerNo { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }
}