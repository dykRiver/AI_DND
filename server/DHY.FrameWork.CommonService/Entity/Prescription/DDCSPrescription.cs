namespace DHY.DDCS.Module.Common.Entity
{
    /// <summary>
    /// DDCS 拆/合方表
    /// </summary>
    [SugarTable(null, "拆/合方表")]
    [SugarIndex("index_{table}_Pid", nameof(Pid), OrderByType.Desc)]
    public sealed class DDCSPrescription : EntityTenant
    {
        /// <summary>
        /// 处方id，与煎药系统对应。对于合方，为其中的一个。
        /// </summary>
        [SugarColumn(ColumnDescription = "处方id")]
        [Required]
        public long Pid { get; set; }

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
        /// 拆分的序号。如拆成2个处方，需要分别为1，2
        /// </summary>
        [SugarColumn(ColumnDescription = "拆分的序号")]
        [Required]
        public int Index { get; set; }

        /// <summary>
        /// 拆分后贴数
        /// </summary>
        [SugarColumn(ColumnDescription = "拆分后贴数")]
        [Required]
        public int Dosage { get; set; }

        /// <summary>
        /// 处方桶类型： 1群药（常规）、2先煎、3后下，4另煎（单独包装）。另包不考虑、烊化不考虑
        /// </summary>
        [SugarColumn(ColumnDescription = "处方桶类型")]
        [Required]
        public ContainerTypeEnum DecoctionType { get; set; }

        /// <summary>
        /// 处方状态 <see cref="PrescriptionStatusEnum"/>
        /// </summary>
        [SugarColumn(ColumnDescription = "处方状态", DefaultValue = "0")]
        [Required]
        public PrescriptionStatusEnum DDCSTaskStatus { get; set; }

        /// <summary>
        /// 拆合方标志。0拆方，1合方（合方一般带着拆方，几个处方合在一起后，依然要按先煎、群药、后下等拆方）。
        /// </summary>
        [SugarColumn(ColumnDescription = "拆合方标志")]
        [Required]
        public byte SplitType { get; set; }

        /// <summary>
        /// 总饮片味数
        /// </summary>
        [SugarColumn(ColumnDescription = "总饮片味数", IsNullable = true)]
        public int? SlicesCount { get; set; }

        /// <summary>
        /// 桶号
        /// </summary>
        [SugarColumn(ColumnDescription = "桶号", IsNullable = true)]
        public long? ContainerNo { get; set; }
        /// <summary>
        /// 先煎桶号
        /// </summary>
        [SugarColumn(ColumnDescription = "先煎桶号", IsNullable = true)]
        public int? DecoctFirstContainerNo { get; set; }
        /// <summary>
        /// 后下桶号
        /// </summary>
        [SugarColumn(ColumnDescription = "后下桶号", IsNullable = true)]
        public int? DecoctLaterContainerNo { get; set; }

        /// <summary>
        /// 储液桶号
        /// </summary>
        [SugarColumn(ColumnDescription = "储液桶号", IsNullable = true)]
        public int? StorageContainerNo { get; set; }

        /// <summary>
        /// 储液桶所在煎药机号
        /// </summary>
        [SugarColumn(ColumnDescription = "储液桶所在煎药机号", IsNullable = true)]
        public int? StorageDecoctorNo { get; set; }

        /// <summary>
        /// 目标得液量
        /// </summary>
        [SugarColumn(ColumnDescription = "目标得液量", IsNullable = true)]
        public int? TargetVolumn { get; set; }

        /// <summary>
        /// 得液重量
        /// </summary>
        [SugarColumn(ColumnDescription = "得液重量", IsNullable = true)]
        public ushort? BrothWeight { get; set; }

        /// <summary>
        /// 煎煮次数
        /// 1-只进行一煎
        /// 2-一煎+二煎
        /// </summary>
        [SugarColumn(ColumnDescription = "煎煮次数", IsNullable = true)]
        public ushort? DecoctNum { get; set; }

        /// <summary>
        /// 关联包装机
        /// 对应包装机的设备号
        /// </summary>
        [SugarColumn(ColumnDescription = "包装机编号", IsNullable = true)]
        public int? PackagingNo { get; set; }

        /// <summary>
        /// 调剂工艺流程Id，空值表示常规方
        /// </summary>
        [SugarColumn(ColumnDescription = "调剂工艺流程Id", IsNullable = true)]
        public long? DispensingProcessesFlowId { get; set; }

        /// <summary>
        /// 煎煮工艺流程Id，空值表示常规方
        /// </summary>
        [SugarColumn(ColumnDescription = "煎煮工艺流程Id", IsNullable = true)]
        public long? DecoctionProcessesFlowId { get; set; }

        /// <summary>
        /// 包装工艺流程Id，空值表示常规方
        /// </summary>
        [SugarColumn(ColumnDescription = "包装工艺流程Id", IsNullable = true)]
        public long? PackingProcessesFlowId { get; set; }

        #region TODO:未来这些内容会被移至工艺里

        /// <summary>
        /// 泡药时间；单位：分钟
        /// </summary>
        [SugarColumn(ColumnDescription = "泡药时间；单位：分钟", IsNullable = true)]
        public int? SoakWaterTime { get; set; }

        /// <summary>
        /// 煎煮时间；单位：分钟
        /// </summary>
        [SugarColumn(ColumnDescription = "煎煮时间；单位：分钟", IsNullable = true)]
        public int? DecoctTime { get; set; }

        /// <summary>
        /// 二煎煎煮时间
        /// </summary>
        [SugarColumn(ColumnDescription = "二煎煎煮时间", IsNullable = true)]
        public int? TwiceDecoctTime { get; set; }

        /// <summary>
        /// 加水量 单位毫升
        /// </summary>
        [SugarColumn(ColumnDescription = "加水量 单位毫升", IsNullable = true)]
        public int? WaterAmount { get; set; }

        /// <summary>
        /// 先煎多加的加水量
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public int? DecoctFirstSuperfluousWaterAmount { get; set; }

        /// <summary>
        /// 二煎加水量
        /// </summary>
        [SugarColumn(ColumnDescription = "二煎加水量", IsNullable = true)]
        public int? TwiceWaterAmount { get; set; }

        /// <summary>
        /// 需要发桶(冲服 兑服 二煎 不需要发桶)
        /// </summary>
        [SugarColumn(ColumnDescription = "是否需要发桶", DefaultValue ="true")]
        public bool NeedDispatchBucket {  get; set; } = true;
        #endregion

        /// <summary>
        /// 处方明细
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        [Navigate(NavigateType.OneToMany, nameof(DDCSPrescriptionDetail.DDCSPid))]
        public List<DDCSPrescriptionDetail> Details { get; set; }

        /// <summary>
        /// 合方列表
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        [Navigate(typeof(DDCSPrescriptionMapping), nameof(DDCSPrescriptionMapping.DDCSPid), nameof(DDCSPrescriptionMapping.Pid))]
        public List<PrescriptionInfo> Prescriptions { get; set; }
    }
}
