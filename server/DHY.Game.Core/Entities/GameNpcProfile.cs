namespace DHY.Game.Core.Entities;

/// <summary>
/// NPC档案卡
/// </summary>
[SugarTable("game_npc_profile", "NPC档案卡")]
public class GameNpcProfile : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// NPC唯一标识 (如mike_bartender)
    /// </summary>
    [SugarColumn(ColumnDescription = "NPC唯一标识", Length = 128)]
    public string NpcIdentifier { get; set; }

    /// <summary>
    /// NPC名称
    /// </summary>
    [SugarColumn(ColumnDescription = "NPC名称", Length = 64)]
    public string Name { get; set; }

    /// <summary>
    /// 角色定位
    /// </summary>
    [SugarColumn(ColumnDescription = "角色定位", Length = 128)]
    public string? Role { get; set; }

    /// <summary>
    /// 性格标签
    /// </summary>
    [SugarColumn(ColumnDescription = "性格标签", Length = 256)]
    public string? Personality { get; set; }

    /// <summary>
    /// 口头禅
    /// </summary>
    [SugarColumn(ColumnDescription = "口头禅", Length = 256)]
    public string? Catchphrase { get; set; }

    /// <summary>
    /// 语言风格描述
    /// </summary>
    [SugarColumn(ColumnDescription = "语言风格描述", Length = 512)]
    public string? LanguageStyle { get; set; }

    /// <summary>
    /// 初始态度 (-5~+5)
    /// </summary>
    [SugarColumn(ColumnDescription = "初始态度", DefaultValue = "0")]
    public int InitialAttitude { get; set; }

    /// <summary>
    /// 当前态度
    /// </summary>
    [SugarColumn(ColumnDescription = "当前态度", DefaultValue = "0")]
    public int CurrentAttitude { get; set; }

    /// <summary>
    /// 所在位置
    /// </summary>
    [SugarColumn(ColumnDescription = "所在位置", Length = 256)]
    public string? Location { get; set; }

    /// <summary>
    /// 是否存活
    /// </summary>
    [SugarColumn(ColumnDescription = "是否存活")]
    public bool IsAlive { get; set; } = true;

    /// <summary>
    /// 行动时间线JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "行动时间线JSON", ColumnDataType = "nvarchar(max)")]
    public string? ActionPlan { get; set; }

    /// <summary>
    /// 交互摘要JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "交互摘要JSON", ColumnDataType = "nvarchar(max)")]
    public string? InteractionHistory { get; set; }

    /// <summary>
    /// 是否核心NPC
    /// </summary>
    [SugarColumn(ColumnDescription = "是否核心NPC", DefaultValue = "0")]
    public bool IsCritical { get; set; }
}
