using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Dtos
{
    public class MedicationGuidanceDetailInput
    {
        /// <summary>
        /// 药嘱条目
        /// </summary>
        public GuidanceType GuidanceType { get; set; }
        /// <summary>
        /// 药嘱内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 处方编号
        /// </summary>
        public string PrescriptionNo { get; set; }
        /// <summary>
        /// 处方Id
        /// </summary>
        public int PrescriptionId { get; set; }
        /// <summary>
        /// 思考过程
        /// </summary>
        public string Think { get; set; }

    }
}
