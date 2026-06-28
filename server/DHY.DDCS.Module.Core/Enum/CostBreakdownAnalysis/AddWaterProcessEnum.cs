namespace DHY.DDCS.Module.Core;

/// <summary>
/// 加水环节
/// </summary>
public enum AddWaterProcessEnum
{
    /// <summary>
    /// 群药加水
    /// </summary>
    [ProcessArea(ProcessAreaEnum.DecoctionArea)]
    Group,
    /// <summary>
    /// 先煎
    /// </summary>
    [ProcessArea(ProcessAreaEnum.DecoctionArea)]
    Pre,
    /// <summary>
    /// 二煎
    /// </summary>
    [ProcessArea(ProcessAreaEnum.DecoctionArea)]
    Twice,
    /// <summary>
    /// 煎药机清洗按压
    /// </summary>
    [ProcessArea(ProcessAreaEnum.DecoctionArea)]
    DecoctorClean,
    /// <summary>
    /// 包装机清洗
    /// </summary>
    [ProcessArea(ProcessAreaEnum.PackingArea)]
    PackerClean,
    /// <summary>
    /// 出药量不足的补水量
    /// </summary>
    [ProcessArea(ProcessAreaEnum.DecoctionArea)]
    Supplement
}
