namespace DHY.Game.Core.Options;

/// <summary>
/// 游戏核心配置选项
/// </summary>
public class GameOptions : IConfigurableOptions
{
    /// <summary>
    /// 最大基础HP
    /// </summary>
    public int MaxBaseHp { get; set; }

    /// <summary>
    /// 每点体质调整值对应的HP加成
    /// </summary>
    public int HpPerConModifier { get; set; }

    /// <summary>
    /// 每日时段数
    /// </summary>
    public int TimeSegmentsPerDay { get; set; }

    /// <summary>
    /// 加时惩罚值
    /// </summary>
    public int OvertimePenalty { get; set; }

    /// <summary>
    /// 重伤阈值百分比
    /// </summary>
    public int WoundThresholdPercent { get; set; }

    /// <summary>
    /// 世界状态重定位间隔(交互次数)
    /// </summary>
    public int RepositionInterval { get; set; }

    /// <summary>
    /// 最大专精技能槽位数
    /// </summary>
    public int MaxExpertiseSlots { get; set; }

    /// <summary>
    /// 副本内最大等级
    /// </summary>
    public int MaxDungeonLevel { get; set; }

    /// <summary>
    /// 评分权重配置
    /// </summary>
    public ScoringWeightsOptions ScoringWeights { get; set; }
}

/// <summary>
/// 评分权重配置
/// </summary>
public class ScoringWeightsOptions
{
    public int MainQuest { get; set; }
    public int Execution { get; set; }
    public int Exploration { get; set; }
    public int Survival { get; set; }
    public int WorldImpact { get; set; }
}
