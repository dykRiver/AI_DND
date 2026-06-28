namespace DHY.MG.Module.Sys.Enum
{
    public enum GuidanceType
    {
        /// <summary>
        /// 服药禁忌
        /// </summary>
        [Description("服药禁忌")]
        Contraindication,

        /// <summary>
        /// 服用时间
        /// </summary>
        [Description("服用时间")]
        UseTime,

        /// <summary>
        /// 服用次数
        /// </summary>
        [Description("服用次数")]
        UseCount,

        /// <summary>
        /// 服用温度
        /// </summary>
        [Description("服用温度")] 
        UseTemperature,

        /// <summary>
        /// 特殊服法
        /// </summary>
        [Description("特殊服法")] 
        SpecialUse,

        /// <summary>
        /// 服用疗程
        /// </summary>
        [Description("服用疗程")] 
        UseCourse,

        /// <summary>
        /// 贮存指导
        /// </summary>
        [Description("贮存指导")] 
        Store,

        /// <summary>
        /// 自我监测
        /// </summary>
        [Description("自我监测")] 
        Monitor,

        /// <summary>
        /// 健康指导
        /// </summary>
        //[Description("健康指导")] 
        //Health,

        /// <summary>
        /// 服用温度
        /// </summary>
        [Description("服用剂量")]
        UseDosage = 9,

        /// <summary>
        /// 健康指导
        /// </summary>
        [Description("证候禁忌")]
        Health1,

        /// <summary>
        /// 健康指导
        /// </summary>
        [Description("饮食禁忌")]
        Health2,

        /// <summary>
        /// 健康指导
        /// </summary>
        [Description("辩证调护")]
        Health3,

        /// <summary>
        /// 人群禁忌
        /// </summary>
        [Description("人群禁忌")]
        Health4,
    }
}
