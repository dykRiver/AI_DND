namespace DHY.DDCS.Module.Core.Enum.Device
{
    /// <summary>
    /// 处方桶类型：0未知、1群药、2二煎群药、3后下群药、4先煎群药、5先煎、6后下、7另煎、8二煎空桶
    /// </summary>
    public enum ContainerCraftTypeEnum : byte
    {
        /// <summary>
        /// 0未知
        /// </summary>
        [Description("未知")]
        Unknown = 0,
        /// <summary>
        /// 1常规群药
        /// </summary>
        [Description("群药")]
        GroupMedicine = 1,
        /// <summary>
        /// 2二煎群药
        /// </summary>
        [Description("二煎群药")]
        TwoFriedGroupMedicine = 2,
        /// <summary>
        /// 3后下群药
        /// </summary>
        [Description("后下群药")]
        DecoctLaterGroupMedicine = 3,
        /// <summary>
        /// 4先煎群药
        /// </summary>
        [Description("先煎群药")]
        DecoctFirstGroupMedicine = 4,
        /// <summary>
        /// 5先煎
        /// </summary>
        [Description("先煎")]
        DecoctFirst = 5,
        /// <summary>
        /// 6后下
        /// </summary>
        [Description("后下")]
        DecoctLater = 6,
        /// <summary>
        /// 7另煎（单独包装）
        /// </summary>
        [Description("另煎")]
        DecoctSeparately = 7,
        /// <summary>
        /// 8二煎空桶
        /// </summary>
        [Description("储液桶")]
        TwoFriedEmptyBuckets = 8,
        

    }
}
