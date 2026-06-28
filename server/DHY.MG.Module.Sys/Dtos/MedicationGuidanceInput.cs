namespace DHY.MG.Module.Sys.Dtos
{
    public class MedicationGuidanceInput
    {
        /// <summary>
        /// 药嘱条目
        /// </summary>
        public List<MedicationGuidanceDetailInput> MedicationGuidance { get; set; }
        /// <summary>
        /// 处方编号
        /// </summary>
        public string PrescriptionNo { get; set; }
        /// <summary>
        /// 处方ID
        /// </summary>
        public int PrescriptionId { get; set; }
    }
}
