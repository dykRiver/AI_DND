namespace DHY.Game.Core.Dtos;

/// <summary>
/// 属性点分配输入
/// </summary>
public class AllocateAttributeInput
{
    /// <summary>用户ID</summary>
    public long UserId { get; set; }
    /// <summary>
    /// 属性分配 如 {"Strength": 1, "Dexterity": 1}
    /// </summary>
    public Dictionary<string, int> Allocations { get; set; } = new();
}

/// <summary>
/// 结算输出(三段式)
/// </summary>
public class SettlementOutput
{
    /// <summary>
    /// 叙事退出
    /// </summary>
    public string ExitNarrative { get; set; } = "";

    /// <summary>
    /// 后日谈
    /// </summary>
    public string Epilogue { get; set; } = "";

    /// <summary>
    /// 精简评语
    /// </summary>
    public string Comment { get; set; } = "";

    /// <summary>
    /// 评分等级
    /// </summary>
    public string ScoreLevel { get; set; } = "";

    /// <summary>
    /// 奖励信息
    /// </summary>
    public RewardInfo Rewards { get; set; } = new();
}

/// <summary>
/// 奖励信息
/// </summary>
public class RewardInfo
{
    /// <summary>
    /// 奖励属性点
    /// </summary>
    public int AttributePoints { get; set; }

    /// <summary>
    /// 奖励技能点
    /// </summary>
    public int SkillPoints { get; set; }

    /// <summary>
    /// Meta经验
    /// </summary>
    public int MetaExp { get; set; }

    /// <summary>
    /// 天赋碎片
    /// </summary>
    public int TalentFragments { get; set; }
}

/// <summary>
/// 天赋树输出
/// </summary>
public class TalentTreeOutput
{
    /// <summary>
    /// 所有节点
    /// </summary>
    public List<TalentNodeOutput> Nodes { get; set; } = new();

    /// <summary>
    /// 可用天赋点
    /// </summary>
    public int AvailablePoints { get; set; }
}

/// <summary>
/// 天赋节点输出
/// </summary>
public class TalentNodeOutput
{
    /// <summary>
    /// 节点路径
    /// </summary>
    public string NodePath { get; set; } = "";

    /// <summary>
    /// 节点名称
    /// </summary>
    public string NodeName { get; set; } = "";

    /// <summary>
    /// 节点效果
    /// </summary>
    public string NodeEffect { get; set; } = "";

    /// <summary>
    /// 路线名称
    /// </summary>
    public string RouteName { get; set; } = "";

    /// <summary>
    /// 位置
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// 是否已解锁
    /// </summary>
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// 是否桥接节点
    /// </summary>
    public bool IsBridge { get; set; }

    /// <summary>
    /// 当前是否可解锁
    /// </summary>
    public bool CanUnlock { get; set; }
}

/// <summary>
/// 段位输出
/// </summary>
public class RankOutput
{
    /// <summary>
    /// 段位等级
    /// </summary>
    public int RankTier { get; set; }

    /// <summary>
    /// 段位名称
    /// </summary>
    public string RankName { get; set; } = "";

    /// <summary>
    /// 是否可晋级
    /// </summary>
    public bool CanPromote { get; set; }

    /// <summary>
    /// 距下一段位副本数
    /// </summary>
    public int DungeonCountToNext { get; set; }

    /// <summary>
    /// 是否正在晋级赛中
    /// </summary>
    public bool IsInPromotion { get; set; }
}

/// <summary>
/// 晋级结果输出
/// </summary>
public class PromotionResultOutput
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 新段位等级
    /// </summary>
    public int NewRankTier { get; set; }

    /// <summary>
    /// 新段位名称
    /// </summary>
    public string NewRankName { get; set; } = "";

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = "";
}
