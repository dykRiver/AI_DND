namespace DHY.Game.Core.Entities;

/// <summary>
/// Meta永久档案
/// </summary>
[SugarTable("game_player_meta", "Meta永久档案")]
public class GamePlayerMeta : EntityBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", DefaultValue = "0")]
    public long UserId { get; set; }

    /// <summary>
    /// Meta等级 (1-30)
    /// </summary>
    [SugarColumn(ColumnDescription = "Meta等级", DefaultValue = "0")]
    public int MetaLevel { get; set; }

    /// <summary>
    /// 经验值
    /// </summary>
    [SugarColumn(ColumnDescription = "经验值", DefaultValue = "0")]
    public int Experience { get; set; }

    /// <summary>
    /// 额外力量 (0-4)
    /// </summary>
    [SugarColumn(ColumnDescription = "额外力量", DefaultValue = "0")]
    public int BonusStrength { get; set; }

    /// <summary>
    /// 额外敏捷
    /// </summary>
    [SugarColumn(ColumnDescription = "额外敏捷", DefaultValue = "0")]
    public int BonusDexterity { get; set; }

    /// <summary>
    /// 额外体质
    /// </summary>
    [SugarColumn(ColumnDescription = "额外体质", DefaultValue = "0")]
    public int BonusConstitution { get; set; }

    /// <summary>
    /// 额外智力
    /// </summary>
    [SugarColumn(ColumnDescription = "额外智力", DefaultValue = "0")]
    public int BonusIntelligence { get; set; }

    /// <summary>
    /// 额外感知
    /// </summary>
    [SugarColumn(ColumnDescription = "额外感知", DefaultValue = "0")]
    public int BonusWisdom { get; set; }

    /// <summary>
    /// 额外魅力
    /// </summary>
    [SugarColumn(ColumnDescription = "额外魅力", DefaultValue = "0")]
    public int BonusCharisma { get; set; }

    /// <summary>
    /// 可用天赋点
    /// </summary>
    [SugarColumn(ColumnDescription = "可用天赋点", DefaultValue = "0")]
    public int TalentPoints { get; set; }

    /// <summary>
    /// 累计完成副本数
    /// </summary>
    [SugarColumn(ColumnDescription = "累计完成副本数", DefaultValue = "0")]
    public int DungeonCount { get; set; }

    /// <summary>
    /// 当前段位记录ID
    /// </summary>
    [SugarColumn(ColumnDescription = "当前段位记录ID", IsNullable = true)]
    public long? CurrentRankId { get; set; }
}
