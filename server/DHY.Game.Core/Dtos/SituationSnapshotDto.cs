using Newtonsoft.Json;

namespace DHY.Game.Core.Dtos;

/// <summary>
/// 局面快照（世界状态结构化Schema）
/// 分类AI读当前状态（过滤change_history），导演AI读全量（含change_history）
/// </summary>
public class SituationSnapshotDto
{
    /// <summary>世界设定（时代/科技/文化/地理/关联地图）</summary>
    [JsonProperty("world_setting")]
    public Dictionary<string, object>? WorldSetting { get; set; }

    /// <summary>当前位置</summary>
    [JsonProperty("location")]
    public string Location { get; set; } = "";

    /// <summary>当前天数</summary>
    [JsonProperty("current_day")]
    public int CurrentDay { get; set; } = 1;

    /// <summary>当前时段（上午/下午/傍晚/夜间）</summary>
    [JsonProperty("current_segment")]
    public string CurrentSegment { get; set; } = "上午";

    /// <summary>玩家位置/姿态（如：隐藏在暗处、居高临下、坐在蛇哥对面）</summary>
    [JsonProperty("player_position")]
    public string PlayerPosition { get; set; } = "";

    /// <summary>玩家状态（伤势、装备暴露情况等）</summary>
    [JsonProperty("player_status")]
    public string PlayerStatus { get; set; } = "正常";

    /// <summary>环境条件（光线、天气、噪音、地形等）</summary>
    [JsonProperty("environment")]
    public string Environment { get; set; } = "";

    /// <summary>NPC状态列表</summary>
    [JsonProperty("npc_states")]
    public List<NpcStateDto> NpcStates { get; set; } = new();

    /// <summary>活跃状态效果（如：中毒、隐身、被追踪）</summary>
    [JsonProperty("active_conditions")]
    public List<string> ActiveConditions { get; set; } = new();

    /// <summary>关键标记（如：已进入战斗状态、警报已触发、主线阶段2）</summary>
    [JsonProperty("flags")]
    public List<string> Flags { get; set; } = new();

    /// <summary>任务进度（主线/支线/隐藏内容的完成状态，导演AI在任务有进展时输出）</summary>
    [JsonProperty("quest_progress")]
    public QuestProgressDto QuestProgress { get; set; } = new();

    /// <summary>变化历史（每轮导演AI的summary摘要，供导演AI推演上下文）</summary>
    [JsonProperty("change_history")]
    public List<ChangeHistoryEntry> ChangeHistory { get; set; } = new();
}

/// <summary>
/// NPC状态
/// </summary>
public class NpcStateDto
{
    /// <summary>NPC标识</summary>
    [JsonProperty("npc_id")]
    public string NpcId { get; set; } = "";

    /// <summary>警觉度（未察觉/警觉/敌对/友善/盟友）</summary>
    [JsonProperty("awareness")]
    public string Awareness { get; set; } = "未察觉";

    /// <summary>身体状态（正常/受伤/倒地/死亡）</summary>
    [JsonProperty("status")]
    public string Status { get; set; } = "正常";

    /// <summary>态度（中立/友善/敌对/试探性等）</summary>
    [JsonProperty("attitude")]
    public string Attitude { get; set; } = "中立";
}

/// <summary>
/// 变化历史条目（记录每轮发生的事件摘要）
/// </summary>
public class ChangeHistoryEntry
{
    /// <summary>交互轮次</summary>
    [JsonProperty("round")]
    public int Round { get; set; }

    /// <summary>本轮事件摘要</summary>
    [JsonProperty("summary")]
    public string Summary { get; set; } = "";
}

/// <summary>
/// 任务进度（结构化评分数据源，导演AI在任务有进展时输出累积状态）
/// </summary>
public class QuestProgressDto
{
    /// <summary>主线状态（in_progress/complete/failed）</summary>
    [JsonProperty("main_quest_status")]
    public string MainQuestStatus { get; set; } = "in_progress";

    /// <summary>主线已完成的关键节点数（从1开始递增）</summary>
    [JsonProperty("main_quest_phase")]
    public int MainQuestPhase { get; set; } = 0;

    /// <summary>已完成的支线任务名列表（累积，必须与建筑师AI生成的支线名精确匹配）</summary>
    [JsonProperty("completed_side_quests")]
    public List<string> CompletedSideQuests { get; set; } = new();

    /// <summary>已解锁的隐藏支线任务名列表（累积，解锁后对玩家可见）</summary>
    [JsonProperty("unlocked_side_quests")]
    public List<string> UnlockedSideQuests { get; set; } = new();

    /// <summary>已发现的隐藏内容名列表（累积，必须与建筑师AI生成的隐藏内容名精确匹配）</summary>
    [JsonProperty("discovered_hidden")]
    public List<string> DiscoveredHidden { get; set; } = new();
}

/// <summary>
/// 世界状态变更DTO（导演AI每轮输出，代码层合并到局面快照）
/// 所有字段均为 nullable：仅变化时才输出，未变化保持上一轮值
/// </summary>
public class WorldStateChangesDto
{
    /// <summary>位置变化</summary>
    [JsonProperty("location")]
    public string? Location { get; set; }

    /// <summary>玩家位置/姿态变化</summary>
    [JsonProperty("player_position")]
    public string? PlayerPosition { get; set; }

    /// <summary>玩家状态变化</summary>
    [JsonProperty("player_status")]
    public string? PlayerStatus { get; set; }

    /// <summary>环境条件变化</summary>
    [JsonProperty("environment")]
    public string? Environment { get; set; }

    /// <summary>NPC状态变化（按npc_id合并更新）</summary>
    [JsonProperty("npc_states")]
    public List<NpcStateDto>? NpcStates { get; set; }

    /// <summary>活跃状态效果变化（全量替换）</summary>
    [JsonProperty("active_conditions")]
    public List<string>? ActiveConditions { get; set; }

    /// <summary>关键标记变化（全量替换）</summary>
    [JsonProperty("flags")]
    public List<string>? Flags { get; set; }

    /// <summary>任务进度变化（仅任务有进展时输出，累积状态，代码层覆盖合并）</summary>
    [JsonProperty("quest_progress")]
    public QuestProgressDto? QuestProgress { get; set; }

    /// <summary>本轮事件摘要（必出，写入change_history）</summary>
    [JsonProperty("summary")]
    public string Summary { get; set; } = "";
}
