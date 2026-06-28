namespace DHY.Game.Core.Entities;

/// <summary>
/// 段位记录
/// </summary>
[SugarTable("game_player_rank", "段位记录")]
public class GamePlayerRank : EntityBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", DefaultValue = "0")]
    public long UserId { get; set; }

    /// <summary>
    /// Meta档案ID
    /// </summary>
    [SugarColumn(ColumnDescription = "Meta档案ID", DefaultValue = "0")]
    public long MetaId { get; set; }

    /// <summary>
    /// 段位等级 (1-7段)
    /// </summary>
    [SugarColumn(ColumnDescription = "段位等级", DefaultValue = "0")]
    public int RankTier { get; set; }

    /// <summary>
    /// 段位名称
    /// </summary>
    [SugarColumn(ColumnDescription = "段位名称", Length = 32)]
    public string RankName { get; set; }

    /// <summary>
    /// 距下次晋级的副本数
    /// </summary>
    [SugarColumn(ColumnDescription = "距下次晋级的副本数", DefaultValue = "0")]
    public int PromotionDungeonCount { get; set; }

    /// <summary>
    /// 是否正在晋级赛中
    /// </summary>
    [SugarColumn(ColumnDescription = "是否正在晋级赛中", DefaultValue = "0")]
    public bool IsInPromotion { get; set; }

    /// <summary>
    /// 晋级尝试次数
    /// </summary>
    [SugarColumn(ColumnDescription = "晋级尝试次数", DefaultValue = "0")]
    public int PromotionAttempts { get; set; }

    /// <summary>
    /// 最后晋级时间
    /// </summary>
    [SugarColumn(ColumnDescription = "最后晋级时间", IsNullable = true)]
    public DateTime? LastPromotionTime { get; set; }
}
