namespace DHY.Game.Core.Entities;

/// <summary>
/// 已知情报/无形资产（纸条内容、电话号码、暗号、记忆等信息载体）
/// 与物理背包 game_inventory_item 互补：物理道具入背包，无形信息入此表。
/// 由道具AI（物资官）作为唯一写入权威，供分类AI做可行性判定与前端"已知线索"展示。
/// </summary>
[SugarTable("game_known_asset", "已知情报/无形资产")]
public class GameKnownAsset : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", DefaultValue = "0")]
    public long CharacterId { get; set; }

    /// <summary>
    /// 资产类型 (情报/线索/联系方式/记忆/暗号)
    /// </summary>
    [SugarColumn(ColumnDescription = "资产类型", Length = 32)]
    public string AssetType { get; set; } = "情报";

    /// <summary>
    /// 名称（如"维修单背面的手写号码"）
    /// </summary>
    [SugarColumn(ColumnDescription = "名称", Length = 128)]
    public string Name { get; set; } = "";

    /// <summary>
    /// 内容（实际信息，如具体号码、纸条文字、暗号内容）
    /// </summary>
    [SugarColumn(ColumnDescription = "内容", ColumnDataType = "nvarchar(max)")]
    public string? Content { get; set; }

    /// <summary>
    /// 获得来源（如"雷哥在维修店交给玩家"）
    /// </summary>
    [SugarColumn(ColumnDescription = "获得来源", Length = 256)]
    public string? Source { get; set; }

    /// <summary>
    /// 获得轮次（交互序号）
    /// </summary>
    [SugarColumn(ColumnDescription = "获得轮次", DefaultValue = "0")]
    public int AcquiredRound { get; set; }

    /// <summary>
    /// 是否有效（false=已失效，如纸条烧毁、号码遗忘）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否有效", DefaultValue = "1")]
    public bool IsValid { get; set; } = true;

    /// <summary>
    /// 时间戳
    /// </summary>
    [SugarColumn(ColumnDescription = "时间戳")]
    public DateTime Timestamp { get; set; }
}
