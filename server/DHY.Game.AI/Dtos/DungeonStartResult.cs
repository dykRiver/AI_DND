namespace DHY.Game.AI.Dtos;

/// <summary>
/// 副本启动结果
/// </summary>
public class DungeonStartResult
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>副本会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>开场叙事</summary>
    public string OpeningNarrative { get; set; } = "";

    /// <summary>世界设定摘要</summary>
    public string WorldSettingSummary { get; set; } = "";

    /// <summary>是否为续玩（已有活跃会话）</summary>
    public bool IsResumed { get; set; }

    /// <summary>副本名称</summary>
    public string DungeonName { get; set; } = "";

    /// <summary>世界背景描述</summary>
    public string WorldBackground { get; set; } = "";

    /// <summary>主线任务目标</summary>
    public string MainQuestObjective { get; set; } = "";

    /// <summary>主线关键节点</summary>
    public List<string> MainQuestNodes { get; set; } = new();

    /// <summary>关键地点</summary>
    public List<string> KeyLocations { get; set; } = new();

    /// <summary>支线任务列表（名称+描述，初始未完成）</summary>
    public List<SideQuestBriefInfo> SideQuests { get; set; } = new();

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 支线任务简要信息（供前端展示）
/// </summary>
public class SideQuestBriefInfo
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
