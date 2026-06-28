using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Entities
{
    /// <summary>
    /// 通用药嘱条目匹配规则
    /// </summary>
    [SugarTable(null, "通用药嘱条目匹配规则")]
    public class CommonMatchRule : EntityTenant
    {
        public CommonMatchRule() { }

        public CommonMatchRule(string drugName, string keyWord, string guideContent, int? ageMin=null, int? ageMax = null, SexType? sex = null, GuidanceType guidanceType = GuidanceType.Health4,  int level = 10, bool isDefault =true)
        {
            GuidanceType = guidanceType;
            KeyWord = keyWord;
            Level = level;
            IsDefault = isDefault;
            GuideContent = guideContent;
            AgeMin = ageMin;
            AgeMax = ageMax;
            DrugName = drugName;
            Sex = sex;
        }


        /// <summary>
        /// 药嘱条目
        /// </summary>
        [SugarColumn(ColumnDescription = "药嘱条目", IsNullable = false)]
        public GuidanceType GuidanceType { get; set; }
        /// <summary>
        /// 关键词
        /// </summary>
        [SugarColumn(ColumnDescription = "关键词", Length = 500, IsNullable = true)]
        public string KeyWord { get; set; }
        /// <summary>
        /// 优先级
        /// </summary>
        [SugarColumn(ColumnDescription = "优先级", DefaultValue = "0", IsNullable = false)]
        public int Level { get; set; }
        /// <summary>
        /// 是否默认
        /// </summary>
        [SugarColumn(ColumnDescription = "是否默认", IsNullable = false)]
        public bool IsDefault { get; set; }
        /// <summary>
        /// 药嘱内容
        /// </summary>
        [SugarColumn(ColumnDescription = "药嘱内容", Length = 500, IsNullable = true)]
        public string GuideContent { get; set; }
        /// <summary>
        /// 年龄限制下限
        /// </summary>
        [SugarColumn(ColumnDescription = "年龄限制下限", IsNullable = true)]
        public int? AgeMin { get; set; }
        /// <summary>
        /// 年龄限制上限
        /// </summary>
        [SugarColumn(ColumnDescription = "年龄限制上限", IsNullable = true)]
        public int? AgeMax { get; set; }
        /// <summary>
        /// 饮片名称
        /// </summary>
        [SugarColumn(ColumnDescription = "饮片名称", Length = 50, IsNullable = true)]
        public string DrugName { get; set; }
        /// <summary>
        /// 性别
        /// </summary>
        [SugarColumn(ColumnDescription = "性别", IsNullable = true)]
        public SexType? Sex { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnDescription = "备注", IsNullable = true)]
        public string Remark { get; set; }

    }

}
