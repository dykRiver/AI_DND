namespace DHY.Game.Core.Entities;

/// <summary>
/// 世界状态快照
/// </summary>
[SugarTable("game_world_state", "世界状态快照")]
public class GameWorldState : EntityBase
{
    /// <summary>
    /// 副本会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "副本会话ID", DefaultValue = "0")]
    public long SessionId { get; set; }

    /// <summary>
    /// 完整状态JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "完整状态JSON", ColumnDataType = "nvarchar(max)")]
    public string? StateJson { get; set; }

    /// <summary>
    /// 快照类型 (current/reposition/history)
    /// </summary>
    [SugarColumn(ColumnDescription = "快照类型", Length = 32)]
    public string SnapshotType { get; set; }

    /// <summary>
    /// 对应第几次交互
    /// </summary>
    [SugarColumn(ColumnDescription = "对应第几次交互", DefaultValue = "0")]
    public int InteractionIndex { get; set; }
}
