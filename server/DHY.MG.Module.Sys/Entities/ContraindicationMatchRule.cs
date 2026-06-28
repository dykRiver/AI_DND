using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Entities
{
    /// <summary>
    /// 通用药嘱条目匹配规则
    /// </summary>
    [SugarTable(null, "通用药嘱条目匹配规则")]
    public class ContraindicationMatchRule : EntityTenant
    {

        /// <summary>
        /// 药嘱条目
        /// </summary>
        [SugarColumn(ColumnDescription = "药嘱条目")]
        public GuidanceType GuidanceType { get; set; }
        /// <summary>
        /// 饮片名称
        /// </summary>
        [SugarColumn(ColumnDescription = "饮片名称", Length = 50)]
        public string DrugName { get; set; }
        /// <summary>
        /// 饮食禁忌
        /// </summary>
        [SugarColumn(ColumnDescription = "饮食禁忌", IsNullable = true)]
        public string Contraindication { get; set; }
        /// <summary>
        /// 妊娠禁用
        /// </summary>
        [SugarColumn(ColumnDescription = "妊娠禁用")]
        [Description("妊娠禁用")]
        public bool IsGestationForbid { get; set; }
        /// <summary>
        /// 妊娠慎用
        /// </summary>
        [SugarColumn(ColumnDescription = "妊娠慎用")]
        [Description("妊娠慎用")]
        public bool IsGestationCautious { get; set; }
        /// <summary>
        /// 哺乳期妇女慎用
        /// </summary>
        [SugarColumn(ColumnDescription = "哺乳期妇女慎用")]
        [Description("哺乳期妇女慎用")]
        public bool IsLactationCautious { get; set; }
        /// <summary>
        /// 女性经期慎用
        /// </summary>
        [SugarColumn(ColumnDescription = "女性经期慎用")]
        [Description("女性经期慎用")]
        public bool IsMenstruationCautious { get; set; }
        /// <summary>
        /// 老年人忌用
        /// </summary>
        [SugarColumn(ColumnDescription = "老年人忌用")]
        [Description("老年人忌用")]
        public bool IsOldForbid { get; set; }
        /// <summary>
        /// 老年人慎用
        /// </summary>
        [SugarColumn(ColumnDescription = "老年人慎用")]
        [Description("老年人慎用")]
        public bool IsOldCautious { get; set; }
        /// <summary>
        /// 老年人不宜长期服用
        /// </summary>
        [SugarColumn(ColumnDescription = "老年人不宜长期服用")]
        [Description("老年人不宜长期服用")]
        public bool IsOldNoLongUse { get; set; }
        /// <summary>
        /// 儿童忌用
        /// </summary>
        [SugarColumn(ColumnDescription = "儿童忌用")]
        [Description("儿童忌用")]
        public bool IsChildForbid { get; set; }
        /// <summary>
        /// 儿童慎用
        /// </summary>
        [SugarColumn(ColumnDescription = "儿童慎用")]
        [Description("儿童慎用")]
        public bool IsChildCautious { get; set; }
        /// <summary>
        /// 儿童不宜长期服用
        /// </summary>
        [SugarColumn(ColumnDescription = "儿童不宜长期服用")]
        [Description("儿童不宜长期服用")]
        public bool IsChildNoLongUse { get; set; }
        /// <summary>
        /// 肝肾功能不全者忌用
        /// </summary>
        [SugarColumn(ColumnDescription = "肝肾功能不全者忌用")]
        [Description("肝肾功能不全者忌用")]
        public bool IsLandKForbid { get; set; }
        /// <summary>
        /// 肝肾功能不全者慎用
        /// </summary>
        [SugarColumn(ColumnDescription = "肝肾功能不全者慎用")]
        [Description("肝肾功能不全者慎用")]
        public bool IsLandKCautious { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnDescription = "备注", IsNullable = true)]
        public string Remark { get; set; }

    }

}
