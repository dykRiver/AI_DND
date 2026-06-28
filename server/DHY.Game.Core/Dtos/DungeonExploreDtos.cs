namespace DHY.Game.Core.Dtos;

/// <summary>
/// 副本模板输出（面向玩家，隐藏BasePrompt等内部管理字段）
/// </summary>
public class DungeonTemplateOutput
{
    public long Id { get; set; }

    /// <summary>
    /// 副本名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// 世界观主题
    /// </summary>
    public string WorldTheme { get; set; } = "";

    /// <summary>
    /// 难度等级 (E/D/C/B/A)
    /// </summary>
    public string Difficulty { get; set; } = "";

    /// <summary>
    /// 世界难度修正值（E=-3/D=-2/C=0/B=+2/A=+3）
    /// </summary>
    public int DifficultyModifier { get; set; }

    /// <summary>
    /// 时间限制天数
    /// </summary>
    public int TimeLimitDays { get; set; }

    /// <summary>
    /// 关键词标签
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// 副本描述
    /// </summary>
    public string? Description { get; set; }
}

/// <summary>
/// 检查活跃会话输出（断线续玩用）
/// </summary>
public class ActiveSessionCheckOutput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>副本模板ID</summary>
    public long TemplateId { get; set; }

    /// <summary>副本名称</summary>
    public string DungeonName { get; set; } = "";

    /// <summary>世界信息（背景+主线任务）</summary>
    public ActiveSessionWorldInfo WorldInfo { get; set; } = new();

    /// <summary>游戏状态</summary>
    public ActiveSessionGameState GameState { get; set; } = new();

    /// <summary>最近叙事记录（用于恢复上下文）</summary>
    public List<ActiveSessionNarrative> RecentNarratives { get; set; } = new();
}

public class ActiveSessionWorldInfo
{
    public string DungeonName { get; set; } = "";
    public string WorldBackground { get; set; } = "";
    public string MainQuestObjective { get; set; } = "";
    public List<string> MainQuestNodes { get; set; } = new();
    public List<string> KeyLocations { get; set; } = new();
}

public class ActiveSessionGameState
{
    public int CurrentHp { get; set; }
    public int MaxHp { get; set; }
    public int HpPercent { get; set; }
    public string Status { get; set; } = "正常";
    public int CurrentDay { get; set; }
    public string CurrentSegment { get; set; } = "上午";
    public int TensionLevel { get; set; }
    public bool IsFatigued { get; set; }
    public bool IsInCombat { get; set; }
}

public class ActiveSessionNarrative
{
    /// <summary>叙事文本</summary>
    public string Text { get; set; } = "";

    /// <summary>类型：narrative/action_result/scene_transition</summary>
    public string ChunkType { get; set; } = "narrative";
}
