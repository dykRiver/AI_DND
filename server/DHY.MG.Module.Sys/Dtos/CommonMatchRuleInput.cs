using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Dtos
{
    public class CommonMatchRuleInput
    {
        /// <summary>
        /// 药嘱条目
        /// </summary>
        public GuidanceType GuidanceType { get; set; }
        /// <summary>
        /// 关键词
        /// </summary>
        public string KeyWord { get; set; }
        /// <summary>
        /// 优先级
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// 是否默认
        /// </summary>
        public bool IsDefault { get; set; }
        /// <summary>
        /// 药嘱内容
        /// </summary>
        public string GuideContent { get; set; }
        /// <summary>
        /// 年龄限制下限
        /// </summary>
        public int? AgeMin { get; set; }
        /// <summary>
        /// 年龄限制上限
        /// </summary>
        public int? AgeMax { get; set; }
        /// <summary>
        /// 饮片名称
        /// </summary>
        public string DrugName { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        public SexType? Sex { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
