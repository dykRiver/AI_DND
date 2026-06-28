using DHY.MG.Module.Sys.Enum;

namespace DHY.MG.Module.Sys.Dtos
{
    public class ContraindicationMatchRuleInput
    {
        /// <summary>
        /// 药嘱条目
        /// </summary>
        public GuidanceType GuidanceType { get; set; }
        /// <summary>
        /// 饮片名称
        /// </summary>
        public string DrugName { get; set; }
        /// <summary>
        /// 饮食禁忌
        /// </summary>
        public string Contraindication { get; set; }
        /// <summary>
        /// 妊娠禁用
        /// </summary>
        public bool IsGestationForbid { get; set; }
        /// <summary>
        /// 妊娠慎用
        /// </summary>
        public bool IsGestationCautious { get; set; }
        /// <summary>
        /// 哺乳期妇女慎用
        /// </summary>
        public bool IsLactationCautious { get; set; }
        /// <summary>
        /// 女性经期慎用
        /// </summary>
        public bool IsMenstruationCautious { get; set; }
        /// <summary>
        /// 老年人禁忌
        /// </summary>
        public bool IsOldForbid { get; set; }
        /// <summary>
        /// 老年人慎用
        /// </summary>
        public bool IsOldCautious { get; set; }
        /// <summary>
        /// 老年人不宜长期服用
        /// </summary>
        public bool IsOldNoLongUse { get; set; }
        /// <summary>
        /// 儿童禁忌
        /// </summary>
        public bool IsChildForbid { get; set; }
        /// <summary>
        /// 儿童慎用
        /// </summary>
        public bool IsChildCautious { get; set; }
        /// <summary>
        /// 儿童不宜长期服用
        /// </summary>
        public bool IsChildNoLongUse { get; set; }
        /// <summary>
        /// 肝肾功能不全者忌用
        /// </summary>
        public bool IsLandKForbid { get; set; }
        /// <summary>
        /// 肝肾功能不全者慎用
        /// </summary>
        public bool IsLandKCautious { get; set; }
        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }
    }
}
