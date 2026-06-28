namespace DHY.Game.Core.Entities;

/// <summary>
/// 副本会话
/// </summary>
[SugarTable("game_dungeon_session", "副本会话")]
public class GameDungeonSession : EntityBase
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", DefaultValue = "0")]
    public long UserId { get; set; }

    /// <summary>
    /// 模板ID
    /// </summary>
    [SugarColumn(ColumnDescription = "模板ID", DefaultValue = "0")]
    public long TemplateId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", DefaultValue = "0")]
    public long CharacterId { get; set; }

    /// <summary>
    /// 状态 (0进行中/1已完成/2已放弃/3死亡/4已挂起)
    /// </summary>
    [SugarColumn(ColumnDescription = "状态", DefaultValue = "0")]
    public int Status { get; set; }

    /// <summary>
    /// 世界设定JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "世界设定JSON", ColumnDataType = "nvarchar(max)")]
    public string? WorldSetting { get; set; }

    /// <summary>
    /// 主线结构JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "主线结构JSON", ColumnDataType = "nvarchar(max)")]
    public string? MainQuest { get; set; }

    /// <summary>
    /// 支线JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "支线JSON", ColumnDataType = "nvarchar(max)")]
    public string? SideQuests { get; set; }

    /// <summary>
    /// 隐藏内容JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "隐藏内容JSON", ColumnDataType = "nvarchar(max)")]
    public string? HiddenContent { get; set; }

    /// <summary>
    /// 难度参数JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "难度参数JSON", ColumnDataType = "nvarchar(max)")]
    public string? DifficultyParams { get; set; }

    /// <summary>
    /// 当前天数
    /// </summary>
    [SugarColumn(ColumnDescription = "当前天数", DefaultValue = "0")]
    public int CurrentDay { get; set; }

    /// <summary>
    /// 当前时段 (0上午/1下午/2傍晚/3夜间)
    /// </summary>
    [SugarColumn(ColumnDescription = "当前时段", DefaultValue = "0")]
    public int CurrentSegment { get; set; }

    /// <summary>
    /// 加时次数
    /// </summary>
    [SugarColumn(ColumnDescription = "加时次数", DefaultValue = "0")]
    public int OvertimeCount { get; set; }

    /// <summary>
    /// 紧张度 (1-10)
    /// </summary>
    [SugarColumn(ColumnDescription = "紧张度", DefaultValue = "0")]
    public int TensionLevel { get; set; }

    /// <summary>
    /// 累计交互次数
    /// </summary>
    [SugarColumn(ColumnDescription = "累计交互次数", DefaultValue = "0")]
    public int InteractionCount { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnDescription = "开始时间")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnDescription = "结束时间", IsNullable = true)]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 同题异卷标记
    /// </summary>
    [SugarColumn(ColumnDescription = "同题异卷标记", DefaultValue = "0")]
    public bool IsReplay { get; set; }
}
