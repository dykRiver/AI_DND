using System.ComponentModel.DataAnnotations;

public class DDCSPrescriptionOutput
{
    public long Id {  get; set; }

    /// <summary>
    /// 处方id，与煎药系统对应。对于合方，为其中的一个。
    /// </summary>
    [Required]
    public long Pid { get; set; }

    /// <summary>
    /// 拆分的序号。如拆成2个处方，需要分别为1，2
    /// </summary>
    [Required]
    public int Index { get; set; }

    /// <summary>
    /// 拆分后贴数
    /// </summary>
    [Required]
    public int Dosage { get; set; }

    /// <summary>
    /// 处方桶类型： 1群药（常规）、2先煎、3后下，4另煎（单独包装）。另包不考虑、烊化不考虑
    /// </summary>
    [Required]
    public ContainerTypeEnum DecoctionType { get; set; }

    /// <summary>
    /// 处方状态 <see cref="PrescriptionStatusEnum"/>
    /// </summary>
    [Required]
    public PrescriptionStatusEnum DDCSTaskStatus { get; set; }

    /// <summary>
    /// 拆合方标志。0拆方，1合方（合方一般带着拆方，几个处方合在一起后，依然要按先煎、群药、后下等拆方）。
    /// </summary>
    [Required]
    public byte SplitType { get; set; }

    /// <summary>
    /// 总饮片味数
    /// </summary>
    public int? SlicesCount { get; set; }

    /// <summary>
    /// 桶号
    /// </summary>
    public int? ContainerNo { get; set; }
    /// <summary>
    /// 先煎桶号
    /// </summary>
    public int? DecoctFirstContainerNo { get; set; }
    /// <summary>
    /// 后下桶号
    /// </summary>
    public int? DecoctLaterContainerNo { get; set; }


    /// <summary>
    /// 储液桶号
    /// </summary>
    public int? StorageContainerNo { get; set; }

    /// <summary>
    /// 储液桶所在煎药机号
    /// </summary>
    public int? StorageDecoctorNo { get; set; }

    /// <summary>
    /// 目标得液量
    /// </summary>
    public int? TargetVolumn { get; set; }

    /// <summary>
    /// 得液重量
    /// </summary>
    public ushort? BrothWeight { get; set; }

    /// <summary>
    /// 煎煮次数
    /// 1-只进行一煎
    /// 2-一煎+二煎
    /// </summary>
    public ushort? DecoctNum { get; set; }

    /// <summary>
    /// 关联包装机
    /// 对应包装机的设备号
    /// </summary>
    public int? PackagingNo { get; set; }

    /// <summary>
    /// 调剂工艺流程Id，空值表示常规方
    /// </summary>
    public long? DispensingProcessesFlowId { get; set; }

    /// <summary>
    /// 煎煮工艺流程Id，空值表示常规方
    /// </summary>
    public long? DecoctionProcessesFlowId { get; set; }

    /// <summary>
    /// 包装工艺流程Id，空值表示常规方
    /// </summary>
    public long? PackingProcessesFlowId { get; set; }

    /// <summary>
    /// 处方明细
    /// </summary>
    public List<DDCSPrescriptionDetailOutput> Details { get; set; }

    /// <summary>
    /// 合方列表
    /// </summary>
    public List<PrescriptionInfoOutput> Prescriptions { get; set; }

    /// <summary>
    /// 泡药时间；单位：分钟
    /// </summary>
    public int SoakWaterTime { get; set; }

    /// <summary>
    /// 煎煮时间；单位：分钟
    /// </summary>
    public int DecoctTime { get; set; }

    /// <summary>
    /// 二煎煎煮时间
    /// </summary>
    public int? TwiceDecoctTime { get; set; }

    /// <summary>
    /// 加水量 单位毫升
    /// </summary>
    public int WaterAmount { get; set; }

    /// <summary>
    /// 二煎加水量
    /// </summary>
    public int? TwiceWaterAmount { get; set; }

    /// <summary>
    /// 医院Id（分配包装机时会用到）
    /// </summary>
    public long HospitalId { get; set; }

    /// <summary>
    /// 优先级
    /// </summary>
    public PriorityEnum Priority { get; set; }
}