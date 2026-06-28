namespace DHY.Game.Core.Entities;

/// <summary>
/// AI调用日志
/// </summary>
[SugarTable("game_ai_call_log", "AI调用日志")]
public class GameAiCallLog : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", IsNullable = true)]
    public long? SessionId { get; set; }

    /// <summary>
    /// AI类型 (classifier/director/narrative/architect)
    /// </summary>
    [SugarColumn(ColumnDescription = "AI类型", Length = 32)]
    public string AiType { get; set; }

    /// <summary>
    /// 模型名称
    /// </summary>
    [SugarColumn(ColumnDescription = "模型名称", Length = 64)]
    public string ModelName { get; set; }

    /// <summary>
    /// 输入Token数
    /// </summary>
    [SugarColumn(ColumnDescription = "输入Token数", DefaultValue = "0")]
    public int InputTokens { get; set; }

    /// <summary>
    /// 输出Token数
    /// </summary>
    [SugarColumn(ColumnDescription = "输出Token数", DefaultValue = "0")]
    public int OutputTokens { get; set; }

    /// <summary>
    /// 总Token数
    /// </summary>
    [SugarColumn(ColumnDescription = "总Token数", DefaultValue = "0")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// 耗时毫秒
    /// </summary>
    [SugarColumn(ColumnDescription = "耗时毫秒", DefaultValue = "0")]
    public int DurationMs { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功", DefaultValue = "0")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 512, IsNullable = true)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 费用估算
    /// </summary>
    [SugarColumn(ColumnDescription = "费用估算", DecimalDigits = 6, DefaultValue = "0")]
    public decimal Cost { get; set; }
}
