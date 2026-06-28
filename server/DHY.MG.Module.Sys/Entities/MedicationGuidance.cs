using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Entities
{
    /// <summary>
    /// 药嘱信息
    /// </summary>
    [SugarTable(null, "药嘱信息")]
    public class MedicationGuidance : EntityTenant
    {

        /// <summary>
        /// 药嘱条目
        /// </summary>
        [SugarColumn(ColumnDescription = "药嘱条目")]
        public GuidanceType GuidanceType { get; set; }
        /// <summary>
        /// 药嘱内容
        /// </summary>
        [SugarColumn(ColumnDescription = "药嘱内容", Length = 500, IsNullable = true)]
        public string Content { get; set; }
        /// <summary>
        /// 处方编号
        /// </summary>
        [SugarColumn(ColumnDescription = "处方编号")]
        public string PrescriptionNo { get; set; }
        /// <summary>
        /// 处方Id
        /// </summary>
        [SugarColumn(ColumnDescription = "处方Id")]
        public int PrescriptionId { get; set; }
        /// <summary>
        /// 思考过程
        /// </summary>
        [SugarColumn(ColumnDescription = "思考过程", IsNullable = true)]
        public string Think { get; set; }

    }

}
