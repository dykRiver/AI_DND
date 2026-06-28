namespace DHY.Game.Core.Entities;

/// <summary>
/// 副本内角色
/// </summary>
[SugarTable("game_character", "副本内角色")]
public class GameCharacter : EntityBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", DefaultValue = "0")]
    public long UserId { get; set; }

    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// 角色名称
    /// </summary>
    [SugarColumn(ColumnDescription = "角色名称", Length = 64)]
    public string Name { get; set; }

    /// <summary>
    /// 力量 (1-20)
    /// </summary>
    [SugarColumn(ColumnDescription = "力量", DefaultValue = "0")]
    public int Strength { get; set; }

    /// <summary>
    /// 敏捷 (1-20)
    /// </summary>
    [SugarColumn(ColumnDescription = "敏捷", DefaultValue = "0")]
    public int Dexterity { get; set; }

    /// <summary>
    /// 体质 (1-20)
    /// </summary>
    [SugarColumn(ColumnDescription = "体质", DefaultValue = "0")]
    public int Constitution { get; set; }

    /// <summary>
    /// 智力 (1-20)
    /// </summary>
    [SugarColumn(ColumnDescription = "智力", DefaultValue = "0")]
    public int Intelligence { get; set; }

    /// <summary>
    /// 感知 (1-20)
    /// </summary>
    [SugarColumn(ColumnDescription = "感知", DefaultValue = "0")]
    public int Wisdom { get; set; }

    /// <summary>
    /// 魅力 (1-20)
    /// </summary>
    [SugarColumn(ColumnDescription = "魅力", DefaultValue = "0")]
    public int Charisma { get; set; }

    /// <summary>
    /// 当前HP
    /// </summary>
    [SugarColumn(ColumnDescription = "当前HP", DefaultValue = "0")]
    public int CurrentHp { get; set; }

    /// <summary>
    /// 最大HP
    /// </summary>
    [SugarColumn(ColumnDescription = "最大HP", DefaultValue = "0")]
    public int MaxHp { get; set; }

    /// <summary>
    /// 副本内等级 (1-4)
    /// </summary>
    [SugarColumn(ColumnDescription = "副本内等级", DefaultValue = "0")]
    public int Level { get; set; }

    /// <summary>
    /// 是否处于战斗中
    /// </summary>
    [SugarColumn(ColumnDescription = "是否处于战斗中", DefaultValue = "0")]
    public bool IsInCombat { get; set; }

    /// <summary>
    /// 是否疲劳
    /// </summary>
    [SugarColumn(ColumnDescription = "是否疲劳", DefaultValue = "0")]
    public bool IsFatigued { get; set; }

    /// <summary>
    /// 是否重伤
    /// </summary>
    [SugarColumn(ColumnDescription = "是否重伤", DefaultValue = "0")]
    public bool IsWounded { get; set; }

    /// <summary>
    /// 是否濒死
    /// </summary>
    [SugarColumn(ColumnDescription = "是否濒死", DefaultValue = "0")]
    public bool IsDying { get; set; }

    /// <summary>
    /// 重伤次数
    /// </summary>
    [SugarColumn(ColumnDescription = "重伤次数", DefaultValue = "0")]
    public int WoundCount { get; set; }

    /// <summary>
    /// 当前位置
    /// </summary>
    [SugarColumn(ColumnDescription = "当前位置", Length = 256)]
    public string? CurrentLocation { get; set; }

    /// <summary>
    /// 背包容量上限（基于STR: 15 + STR调整值）
    /// </summary>
    [SugarColumn(ColumnDescription = "背包容量上限", DefaultValue = "15")]
    public int WeightCapacity { get; set; } = 15;

    /// <summary>
    /// 当前装备武器ID
    /// </summary>
    [SugarColumn(ColumnDescription = "装备武器ID", IsNullable = true)]
    public long? EquippedWeaponId { get; set; }

    /// <summary>
    /// 当前装备防具ID
    /// </summary>
    [SugarColumn(ColumnDescription = "装备防具ID", IsNullable = true)]
    public long? EquippedArmorId { get; set; }
}
