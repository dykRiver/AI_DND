namespace DHY.Game.Core.Entities;

/// <summary>
/// 基础技能
/// </summary>
[SugarTable("game_base_skill", "基础技能")]
[SugarIndex("idx_base_skill_cid_name", nameof(CharacterId), OrderByType.Asc, nameof(SkillName), OrderByType.Asc, true)]
public class GameBaseSkill : EntityBase
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", DefaultValue = "0")]
    public long CharacterId { get; set; }

    /// <summary>
    /// 技能名称
    /// </summary>
    [SugarColumn(ColumnDescription = "技能名称", Length = 64)]
    public string SkillName { get; set; }

    /// <summary>
    /// 关联属性
    /// </summary>
    [SugarColumn(ColumnDescription = "关联属性", Length = 32)]
    public string LinkedAttribute { get; set; }

    /// <summary>
    /// 技能等级 (0-3)
    /// </summary>
    [SugarColumn(ColumnDescription = "技能等级", DefaultValue = "0")]
    public int Level { get; set; }

    /// <summary>
    /// 计算加值
    /// </summary>
    [SugarColumn(ColumnDescription = "计算加值", DefaultValue = "0")]
    public int Bonus { get; set; }
}
