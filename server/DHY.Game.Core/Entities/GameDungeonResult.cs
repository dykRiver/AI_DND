namespace DHY.Game.Core.Entities;

/// <summary>
/// 副本结算
/// </summary>
[SugarTable("game_dungeon_result", "副本结算")]
public class GameDungeonResult : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", DefaultValue = "0")]
    public long UserId { get; set; }

    /// <summary>
    /// 评分等级 (F/E/D/C/B/A/S/SS/SSS)
    /// </summary>
    [SugarColumn(ColumnDescription = "评分等级", Length = 8)]
    public string ScoreLevel { get; set; }

    /// <summary>
    /// 主线任务分 (0-100)
    /// </summary>
    [SugarColumn(ColumnDescription = "主线任务分", DefaultValue = "0")]
    public int MainQuestScore { get; set; }

    /// <summary>
    /// 执行力分
    /// </summary>
    [SugarColumn(ColumnDescription = "执行力分", DefaultValue = "0")]
    public int ExecutionScore { get; set; }

    /// <summary>
    /// 探索分
    /// </summary>
    [SugarColumn(ColumnDescription = "探索分", DefaultValue = "0")]
    public int ExplorationScore { get; set; }

    /// <summary>
    /// 生存分
    /// </summary>
    [SugarColumn(ColumnDescription = "生存分", DefaultValue = "0")]
    public int SurvivalScore { get; set; }

    /// <summary>
    /// 世界影响分
    /// </summary>
    [SugarColumn(ColumnDescription = "世界影响分", DefaultValue = "0")]
    public int WorldImpactScore { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [SugarColumn(ColumnDescription = "总分", DefaultValue = "0")]
    public int TotalScore { get; set; }

    /// <summary>
    /// 奖励属性点
    /// </summary>
    [SugarColumn(ColumnDescription = "奖励属性点", DefaultValue = "0")]
    public int RewardAttributePoints { get; set; }

    /// <summary>
    /// 奖励技能点
    /// </summary>
    [SugarColumn(ColumnDescription = "奖励技能点", DefaultValue = "0")]
    public int RewardSkillPoints { get; set; }

    /// <summary>
    /// 奖励Meta经验
    /// </summary>
    [SugarColumn(ColumnDescription = "奖励Meta经验", DefaultValue = "0")]
    public int RewardMetaExp { get; set; }

    /// <summary>
    /// 奖励天赋碎片
    /// </summary>
    [SugarColumn(ColumnDescription = "奖励天赋碎片", DefaultValue = "0")]
    public int RewardTalentFragments { get; set; }

    /// <summary>
    /// 后日谈叙事
    /// </summary>
    [SugarColumn(ColumnDescription = "后日谈叙事", ColumnDataType = "nvarchar(max)")]
    public string? EpilogueNarrative { get; set; }

    /// <summary>
    /// 精简评语
    /// </summary>
    [SugarColumn(ColumnDescription = "精简评语", Length = 512)]
    public string? SettlementComment { get; set; }
}
