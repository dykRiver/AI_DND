namespace DHY.Game.Core.Entities;

/// <summary>
/// 副本模板
/// </summary>
[SugarTable("game_dungeon_template", "副本模板")]
public class GameDungeonTemplate : EntityBase
{
    /// <summary>
    /// 副本名称
    /// </summary>
    [SugarColumn(ColumnDescription = "副本名称", Length = 128)]
    public string Name { get; set; }

    /// <summary>
    /// 世界观主题
    /// </summary>
    [SugarColumn(ColumnDescription = "世界观主题", Length = 64)]
    public string WorldTheme { get; set; }

    /// <summary>
    /// 难度等级 (E/D/C/B/A)
    /// </summary>
    [SugarColumn(ColumnDescription = "难度等级", Length = 8)]
    public string Difficulty { get; set; }

    /// <summary>
    /// 时间限制天数 (3-7)
    /// </summary>
    [SugarColumn(ColumnDescription = "时间限制天数", DefaultValue = "3")]
    public int TimeLimitDays { get; set; }

    /// <summary>
    /// 关键词标签
    /// </summary>
    [SugarColumn(ColumnDescription = "关键词标签", ColumnDataType = "nvarchar(max)", IsJson = true)]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 副本描述
    /// </summary>
    [SugarColumn(ColumnDescription = "副本描述", ColumnDataType = "nvarchar(max)")]
    public string? Description { get; set; }

    /// <summary>
    /// 给建筑师AI的基础prompt
    /// </summary>
    [SugarColumn(ColumnDescription = "基础Prompt", ColumnDataType = "nvarchar(max)")]
    public string? BasePrompt { get; set; }

    /// <summary>
    /// 副本内升级上限
    /// </summary>
    [SugarColumn(ColumnDescription = "副本内升级上限", DefaultValue = "0")]
    public int MaxLevel { get; set; }

    /// <summary>
    /// 世界难度修正值（影响骰子判定DC，E=-3/D=-2/C=0/B=+2/A=+3）
    /// </summary>
    [SugarColumn(ColumnDescription = "世界难度修正值", DefaultValue = "0")]
    public int DifficultyModifier { get; set; }

    /// <summary>
    /// 文风引导-语调基调（可选，作为架构师生成 style_bible.tone 的基准）
    /// </summary>
    [SugarColumn(ColumnDescription = "文风引导-语调基调", Length = 512, IsNullable = true)]
    public string? Tone { get; set; }

    /// <summary>
    /// 文风引导-感官印象种子（可选，作为 style_bible.sensory_palette 的基准）
    /// </summary>
    [SugarColumn(ColumnDescription = "文风引导-感官印象种子", ColumnDataType = "nvarchar(max)", IsJson = true, IsNullable = true)]
    public List<string>? SensorySeeds { get; set; }

    /// <summary>
    /// 文风引导-禁用陈词（可选，作为 style_bible.forbidden_cliches 的基准）
    /// </summary>
    [SugarColumn(ColumnDescription = "文风引导-禁用陈词", ColumnDataType = "nvarchar(max)", IsJson = true, IsNullable = true)]
    public List<string>? ForbiddenCliches { get; set; }

    /// <summary>
    /// 文风引导-意象种子（可选，作为 motifs 生成的基准）
    /// </summary>
    [SugarColumn(ColumnDescription = "文风引导-意象种子", ColumnDataType = "nvarchar(max)", IsJson = true, IsNullable = true)]
    public List<string>? MotifSeeds { get; set; }
}
