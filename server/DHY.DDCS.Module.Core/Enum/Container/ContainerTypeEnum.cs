/// <summary>
/// 处方桶类型： 0空桶、1群药（常规）、2先煎、3后下，4另煎（单独包装），5二煎空桶。另包不考虑、烊化不考虑
/// </summary>
public enum ContainerTypeEnum : byte
{
    /// <summary>
    /// 0空桶
    /// </summary>
    [Description("空桶")]
    Unknown = 0,
    /// <summary>
    /// 1群药（常规）
    /// </summary>
    [Description("群药")]
    GroupMedicine = 1,
    /// <summary>
    /// 2先煎
    /// DecoctFirst是直译，国际中医文献中的标准术语。
    /// </summary>
    [Description("先煎")]
    DecoctFirst = 2,
    /// <summary>
    /// 3后下
    /// DecoctLater是直译，国际中医文献中广泛使用
    /// </summary>
    [Description("后下")]
    DecoctLater = 3,
    /// <summary>
    /// 4另煎（单独包装） 
    /// DecoctSeparately是强调药材需提前单独煎煮（如矿物类、贝壳类或毒性药材）
    /// </summary>
    [Description("另煎")]
    DecoctSeparately = 4,
    /// <summary>
    /// 5二煎空桶
    /// </summary>
    [Description("二煎空桶")]
    SecondDecoctionEmptyContainer = 100
}