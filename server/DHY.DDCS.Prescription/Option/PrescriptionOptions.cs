using Furion.ConfigurableOptions;

namespace DHY.DDCS.Module.Prescription.Option
{
    public class PrescriptionOptions : IConfigurableOptions
    {
        /// <summary>
        /// 启用拆方
        /// </summary>
        public bool EnablePrescriptionSplit {  get; set; } = true;

        /// <summary>
        ///  拆方最大剂量
        /// </summary>
        public int? SplitMaxDosage { get; set; }

        /// <summary>
        /// 拆方最大重量
        /// </summary>
        public decimal? SplitMaxWeight { get; set; }

        /// <summary>
        /// 开启按煎煮时长拆方
        /// </summary>
        public bool DecoctionTimeSplit { get; set; }

        /// <summary>
        /// 不拆方的类型
        /// </summary>
        public int[] ExceptSplitType {  get; set; }
        public string SplitProvider { get; set; }

        /// <summary>
        /// 拆方是否推送至调剂
        /// </summary>
        public bool PushToDispensing { get; set; } = true;
        /// <summary>
        /// 拆方是否推送至管理系统
        /// </summary>
        public bool PushToManagementSystem { get; set; } = true;
    }
}
