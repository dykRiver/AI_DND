namespace DHY.Game.Core.Entities;

/// <summary>
/// 时段记录
/// </summary>
[SugarTable("game_time_segment", "时段记录")]
public class GameTimeSegment : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// 天数
    /// </summary>
    [SugarColumn(ColumnDescription = "天数", DefaultValue = "0")]
    public int Day { get; set; }

    /// <summary>
    /// 时段 (0-3)
    /// </summary>
    [SugarColumn(ColumnDescription = "时段", DefaultValue = "0")]
    public int Segment { get; set; }

    /// <summary>
    /// 行动摘要
    /// </summary>
    [SugarColumn(ColumnDescription = "行动摘要", ColumnDataType = "nvarchar(max)")]
    public string? ActionSummary { get; set; }

    /// <summary>
    /// HP变化
    /// </summary>
    [SugarColumn(ColumnDescription = "HP变化", DefaultValue = "0")]
    public int HpChange { get; set; }

    /// <summary>
    /// 是否加时
    /// </summary>
    [SugarColumn(ColumnDescription = "是否加时", DefaultValue = "0")]
    public bool IsOvertime { get; set; }
}
