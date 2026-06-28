namespace DHY.Game.Core.Entities;

/// <summary>
/// 专精技能
/// </summary>
[SugarTable("game_expertise_skill", "专精技能")]
public class GameExpertiseSkill : EntityBase
{
    /// <summary>
    /// 角色ID (可null表示Meta层)
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = true)]
    public long? CharacterId { get; set; }

    /// <summary>
    /// Meta档案ID
    /// </summary>
    [SugarColumn(ColumnDescription = "Meta档案ID", IsNullable = true)]
    public long? MetaId { get; set; }

    /// <summary>
    /// 技能名称
    /// </summary>
    [SugarColumn(ColumnDescription = "技能名称", Length = 64)]
    public string SkillName { get; set; }

    /// <summary>
    /// 技能类型
    /// </summary>
    [SugarColumn(ColumnDescription = "技能类型", Length = 32)]
    public string SkillType { get; set; }

    /// <summary>
    /// 技能等级 (1-3)
    /// </summary>
    [SugarColumn(ColumnDescription = "技能等级", DefaultValue = "0")]
    public int Level { get; set; }

    /// <summary>
    /// 等级效果描述
    /// </summary>
    [SugarColumn(ColumnDescription = "等级效果描述", Length = 512)]
    public string? LevelEffect { get; set; }

    /// <summary>
    /// 学习来源 (NPC/书籍/顿悟)
    /// </summary>
    [SugarColumn(ColumnDescription = "学习来源", Length = 64)]
    public string? LearnSource { get; set; }

    /// <summary>
    /// 学习时间
    /// </summary>
    [SugarColumn(ColumnDescription = "学习时间")]
    public DateTime LearnTime { get; set; }

    /// <summary>
    /// 槽位索引 (0-9)
    /// </summary>
    [SugarColumn(ColumnDescription = "槽位索引", DefaultValue = "0")]
    public int SlotIndex { get; set; }

    /// <summary>
    /// 是否激活
    /// </summary>
    [SugarColumn(ColumnDescription = "是否激活", DefaultValue = "0")]
    public bool IsActive { get; set; }
}
