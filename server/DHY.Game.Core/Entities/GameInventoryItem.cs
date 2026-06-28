namespace DHY.Game.Core.Entities;

/// <summary>
/// 背包道具
/// </summary>
[SugarTable("game_inventory_item", "背包道具")]
public class GameInventoryItem : EntityBase
{
    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", DefaultValue = "0")]
    public long CharacterId { get; set; }

    /// <summary>
    /// 道具名称
    /// </summary>
    [SugarColumn(ColumnDescription = "道具名称", Length = 128)]
    public string ItemName { get; set; }

    /// <summary>
    /// 道具类型 (武器/防具/消耗品/关键道具/杂物)
    /// </summary>
    [SugarColumn(ColumnDescription = "道具类型", Length = 32)]
    public string ItemType { get; set; }

    /// <summary>
    /// 道具描述
    /// </summary>
    [SugarColumn(ColumnDescription = "道具描述", Length = 512)]
    public string? Description { get; set; }

    /// <summary>
    /// 数量
    /// </summary>
    [SugarColumn(ColumnDescription = "数量", DefaultValue = "0")]
    public int Quantity { get; set; }

    /// <summary>
    /// 是否已装备
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已装备", DefaultValue = "0")]
    public bool IsEquipped { get; set; }

    /// <summary>
    /// 是否关键道具 (不可丢弃)
    /// </summary>
    [SugarColumn(ColumnDescription = "是否关键道具", DefaultValue = "0")]
    public bool IsKeyItem { get; set; }

    /// <summary>
    /// 道具属性JSON（遗留兼容，新逻辑使用结构化字段）
    /// </summary>
    [SugarColumn(ColumnDescription = "道具属性JSON", ColumnDataType = "nvarchar(max)")]
    public string? Properties { get; set; }

    /// <summary>
    /// 重量单位（0=无重量，如关键道具；支持1位小数）
    /// </summary>
    [SugarColumn(ColumnDescription = "重量单位", DefaultValue = "0", ColumnDataType = "decimal(10,1)")]
    public decimal Weight { get; set; }

    /// <summary>
    /// 属性加值（装备时生效，0=无加值）
    /// </summary>
    [SugarColumn(ColumnDescription = "属性加值", DefaultValue = "0")]
    public int AttributeBonus { get; set; }

    /// <summary>
    /// 关联属性 (STR/DEX/CON/INT/WIS/CHA)
    /// </summary>
    [SugarColumn(ColumnDescription = "关联属性", Length = 16, IsNullable = true)]
    public string? LinkedAttribute { get; set; }

    /// <summary>
    /// 最大使用次数（0=无限，由IsUnlimited控制）
    /// </summary>
    [SugarColumn(ColumnDescription = "最大使用次数", DefaultValue = "0")]
    public int MaxUses { get; set; }

    /// <summary>
    /// 当前剩余使用次数
    /// </summary>
    [SugarColumn(ColumnDescription = "当前剩余次数", DefaultValue = "0")]
    public int CurrentUses { get; set; }

    /// <summary>
    /// 是否无限使用（true=无限，如匕首；false=有限，如手枪）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否无限使用", DefaultValue = "0")]
    public bool IsUnlimited { get; set; }
}
