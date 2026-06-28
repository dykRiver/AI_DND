using System.Diagnostics;

namespace DHY.DDCS.Module.Common.Entity
{
    /// <summary>
    /// 拆方药品表
    /// </summary>
    [SugarTable(null, "拆方药品表")]
    [SugarIndex("index_{table}_dDCSPid", nameof(DDCSPid), OrderByType.Desc)]
    [DebuggerDisplay("药:{Name} 量:{Weight}/{Unit} 煎药方式:{DecoctionType} 序号:{Index}")]
    public sealed class DDCSPrescriptionDetail : PrescriptionDetail, ICloneable
    {
        /// <summary>
        /// 拆方Id
        /// </summary>
        [SugarColumn(ColumnDescription = "拆方Id")]
        [Required]
        public long DDCSPid { get; set; }
        /// <summary>
        /// 拆分的序号。如拆成2个处方，序号分别为1，2。如果一个处方只是按先煎、群药、后下拆分，序号都是1。冗余。
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
        /// 调剂人/设备号
        /// </summary>
        [SugarColumn(ColumnDescription = "调剂人/设备号", IsNullable = true)]
        public string AdjustNum { get; set; }

        /// <summary>
        /// 0=自动调剂，1=人工调剂
        /// </summary>
        [SugarColumn(ColumnDescription = "自动/人工调剂", IsNullable = true)]
        public bool IsAuto { get; set; }

        /// <summary>
        /// 调剂时间
        /// </summary>
        [SugarColumn(ColumnDescription = "调剂时间", IsNullable = true)]
        public DateTime? AdjustTime { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
