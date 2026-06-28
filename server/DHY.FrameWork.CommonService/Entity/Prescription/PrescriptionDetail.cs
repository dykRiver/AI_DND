namespace DHY.DDCS.Module.Common.Entity
{
    /// <summary>
    /// 处方药品表
    /// </summary>
    [SugarTable(null, "原始处方明细")]
    [SugarIndex("index_{table}_DrugId", nameof(DrugId), OrderByType.Desc)]
    [SugarIndex("index_{table}_DrugN", nameof(Name), OrderByType.Asc)]
    public class PrescriptionDetail : EntityTenant
    {
        /// <summary>
        /// 关联原始处方ID
        /// </summary>
        [SugarColumn(ColumnDescription = "原始处方ID")]
        public long Pid { get; set; }

        /// <summary>
        /// 药品Id，与煎药系统对应
        /// </summary>
        [SugarColumn(ColumnDescription = "药品Id")]
        
        public long DrugId { get; set; }

        /// <summary>
        /// 本厂药品编码
        /// </summary>
        [SugarColumn(ColumnDescription = "本厂药品编码", Length = 24)]

        public string Code { get; set; }

        /// <summary>
        /// 处方桶类型(特煎类型)： 1群药（常规）、2先煎、3后下，4另煎（单独包装）。另包不考虑、烊化不考虑
        /// </summary>
        [SugarColumn(ColumnDescription = "处方桶类型")]
        
        public ContainerTypeEnum DecoctionType { get; set; }

        /// <summary>
        /// 本厂药品名称
        /// </summary>
        [SugarColumn(ColumnDescription = "本厂药品名称", Length = 100)]
        [Required, MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// 药品规格
        /// </summary>
        [SugarColumn(ColumnDescription = "药品规格", Length = 200, IsNullable = true)]
        [Required, MaxLength(200)]
        public string? Specification { get; set; }

        /// <summary>
        /// 药品单位
        /// </summary>
        [SugarColumn(ColumnDescription = "药品单位", Length = 24)]
        [Required, MaxLength(24)]
        public string? Unit { get; set; }

        /// <summary>
        ///单剂量；单位：g
        /// </summary>
        [SugarColumn(ColumnDescription = "单剂量", Length = 18, DecimalDigits = 3)]
        
        public decimal SingleDosage { get; set; }

        /// <summary>
        /// 总剂量；单位：g
        /// </summary>
        [SugarColumn(ColumnDescription = "总剂量", Length = 18, DecimalDigits = 3)]
        
        public decimal Weight { get; set; }

        /// <summary>
        /// 吸水比
        /// </summary>
        [SugarColumn(ColumnDescription = "吸水比", Length = 18, DecimalDigits = 3)]
        
        public decimal? WaterAbsorptionRatio { get; set; }

        /// <summary>
        /// 加水量
        /// </summary>
        [SugarColumn(ColumnDescription = "加水量", IsNullable = true)]
        public int? WaterAmount { get; set; }

        /// <summary>
        /// 泡药时间；单位：分钟
        /// </summary>
        [SugarColumn(ColumnDescription = "泡药时间", IsNullable = true)]
        public int? SoakWaterTime { get; set; }

        /// <summary>
        /// 煎煮时间；单位：分钟
        /// </summary>
        [SugarColumn(ColumnDescription = "煎煮时间", IsNullable = true)]
        public int? DecoctionTime { get; set; }
    }
}
