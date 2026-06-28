namespace DHY.Game.Core.Entities;

/// <summary>
/// 叙事日志
/// </summary>
[SugarTable("game_narrative_log", "叙事日志")]
public class GameNarrativeLog : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// 交互序号
    /// </summary>
    [SugarColumn(ColumnDescription = "交互序号", DefaultValue = "0")]
    public int InteractionIndex { get; set; }

    /// <summary>
    /// 玩家输入
    /// </summary>
    [SugarColumn(ColumnDescription = "玩家输入", ColumnDataType = "nvarchar(max)")]
    public string? PlayerInput { get; set; }

    /// <summary>
    /// 导演AI输出JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "导演AI输出JSON", ColumnDataType = "nvarchar(max)")]
    public string? DirectorOutput { get; set; }

    /// <summary>
    /// 最终叙事文本
    /// </summary>
    [SugarColumn(ColumnDescription = "最终叙事文本", ColumnDataType = "nvarchar(max)")]
    public string? NarrativeText { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    [SugarColumn(ColumnDescription = "时间戳")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 是否成人内容
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成人内容", DefaultValue = "0")]
    public bool IsAdult { get; set; }
}
