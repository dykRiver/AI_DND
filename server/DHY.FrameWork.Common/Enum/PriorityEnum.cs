/// <summary>
/// 任务优先级
/// </summary>
public enum PriorityEnum
{
    /// <summary>
    /// 最低
    /// </summary>
    Lowest = 10,
    /// <summary>
    /// 较低-
    /// </summary>
    LowerMinus = 20,
    /// <summary>
    /// 较低
    /// </summary>
    Lower = 30,
    /// <summary>
    /// 较低+
    /// </summary>
    LowerPlus = 40,
    /// <summary>
    /// 中；推荐默认值
    /// </summary>
    Medium = 50,
    /// <summary>
    /// 中+；推荐默认值
    /// </summary>
    MediumPlus = 55,
    /// <summary>
    /// 较高-
    /// </summary>
    HigherMinus = 60,
    /// <summary>
    /// 较高
    /// </summary>
    Higher = 70,
    /// <summary>
    /// 较高+
    /// </summary>
    HigherPlus = 80,

    /// <summary>
    /// 最高-
    /// </summary>
    HighestMinus = 85,

    /// <summary>
    /// 最高
    /// </summary>
    Highest = 90,

    /// <summary>
    /// 比最高还高
    /// </summary>
    Special = 100
}