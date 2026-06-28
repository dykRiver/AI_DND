namespace DHY.Game.Core.Entities;

/// <summary>
/// 天赋树节点
/// </summary>
[SugarTable("game_talent_node", "天赋树节点")]
public class GameTalentNode : EntityBase
{
    /// <summary>
    /// Meta档案ID
    /// </summary>
    [SugarColumn(ColumnDescription = "Meta档案ID", DefaultValue = "0")]
    public long MetaId { get; set; }

    /// <summary>
    /// 节点路径 (路线+位置如"combat_3")
    /// </summary>
    [SugarColumn(ColumnDescription = "节点路径", Length = 64)]
    public string NodePath { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [SugarColumn(ColumnDescription = "节点名称", Length = 64)]
    public string NodeName { get; set; }

    /// <summary>
    /// 效果描述
    /// </summary>
    [SugarColumn(ColumnDescription = "效果描述", Length = 512)]
    public string? NodeEffect { get; set; }

    /// <summary>
    /// 是否已解锁
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已解锁", DefaultValue = "0")]
    public bool IsUnlocked { get; set; }

    /// <summary>
    /// 是否桥接节点
    /// </summary>
    [SugarColumn(ColumnDescription = "是否桥接节点", DefaultValue = "0")]
    public bool IsBridge { get; set; }

    /// <summary>
    /// 路线名称 (4路线名之一)
    /// </summary>
    [SugarColumn(ColumnDescription = "路线名称", Length = 32)]
    public string RouteName { get; set; }

    /// <summary>
    /// 节点在路线上的位置
    /// </summary>
    [SugarColumn(ColumnDescription = "节点在路线上的位置", DefaultValue = "0")]
    public int Position { get; set; }
}
