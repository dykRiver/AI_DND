namespace DHY.Game.Core.Entities;

/// <summary>
/// 骰子判定记录
/// </summary>
[SugarTable("game_dice_roll_record", "骰子判定记录")]
public class GameDiceRollRecord : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// 技能名称
    /// </summary>
    [SugarColumn(ColumnDescription = "技能名称", Length = 64)]
    public string? SkillName { get; set; }

    /// <summary>
    /// 属性名称
    /// </summary>
    [SugarColumn(ColumnDescription = "属性名称", Length = 32)]
    public string? AttributeName { get; set; }

    /// <summary>
    /// D20骰子点数
    /// </summary>
    [SugarColumn(ColumnDescription = "D20骰子点数", DefaultValue = "0")]
    public int D20Roll { get; set; }

    /// <summary>
    /// 调整值
    /// </summary>
    [SugarColumn(ColumnDescription = "调整值", DefaultValue = "0")]
    public int Modifier { get; set; }

    /// <summary>
    /// 总计
    /// </summary>
    [SugarColumn(ColumnDescription = "总计", DefaultValue = "0")]
    public int Total { get; set; }

    /// <summary>
    /// 难度等级（分类AI给出的原始DC）
    /// </summary>
    [SugarColumn(ColumnDescription = "难度等级", DefaultValue = "0")]
    public int DC { get; set; }

    /// <summary>
    /// 世界难度修正值（副本模板难度对应，E=-3/D=-2/C=0/B=+2/A=+3）
    /// </summary>
    [SugarColumn(ColumnDescription = "世界难度修正值", DefaultValue = "0")]
    public int WorldDifficultyModifier { get; set; }

    /// <summary>
    /// 有效DC（原始DC + 世界难度修正）
    /// </summary>
    [SugarColumn(ColumnDescription = "有效DC", DefaultValue = "0")]
    public int EffectiveDC { get; set; }

    /// <summary>
    /// 是否有优势
    /// </summary>
    [SugarColumn(ColumnDescription = "是否有优势", DefaultValue = "0")]
    public bool HasAdvantage { get; set; }

    /// <summary>
    /// 是否有劣势
    /// </summary>
    [SugarColumn(ColumnDescription = "是否有劣势", DefaultValue = "0")]
    public bool HasDisadvantage { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功", DefaultValue = "0")]
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 是否自然20
    /// </summary>
    [SugarColumn(ColumnDescription = "是否自然20", DefaultValue = "0")]
    public bool IsNatural20 { get; set; }

    /// <summary>
    /// 是否自然1
    /// </summary>
    [SugarColumn(ColumnDescription = "是否自然1", DefaultValue = "0")]
    public bool IsNatural1 { get; set; }

    /// <summary>
    /// 叙事摘要
    /// </summary>
    [SugarColumn(ColumnDescription = "叙事摘要", ColumnDataType = "nvarchar(max)")]
    public string? NarrativeSummary { get; set; }
}
