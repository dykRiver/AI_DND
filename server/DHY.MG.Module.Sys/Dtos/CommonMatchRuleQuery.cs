using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Dtos
{
    public class CommonMatchRuleQuery : BasePageInput
    {
        /// <summary>
        /// 主键
        /// </summary>
        public long? Id { get; set; }
        /// <summary>
        /// 药嘱条目
        /// </summary>
        public List<GuidanceType> GuidanceType { get; set; } = new List<GuidanceType>();
        /// <summary>
        /// 关键词
        /// </summary>
        public string KeyWord { get; set; }
        /// <summary>
        /// 优先级
        /// </summary>
        public int? Level { get; set; }
        /// <summary>
        /// 是否默认
        /// </summary>
        public bool? IsDefault { get; set; }
        /// <summary>
        /// 药嘱内容
        /// </summary>
        public string GuideContent { get; set; }

        /// <summary>
        /// 处方Json
        /// </summary>
        public string PrescriptionJson { get; set; }
        /// <summary>
        /// 处方Json
        /// </summary>
        public string PrescriptionNo { get; set; }
        /// <summary>
        /// 处方Id
        /// </summary>
        public long PrescriptionId { get; set; }
    }

    public class CommonMatchRulesQuery
    {
        public List<CommonMatchRuleQuery> querys { get; set; }
    }
}
