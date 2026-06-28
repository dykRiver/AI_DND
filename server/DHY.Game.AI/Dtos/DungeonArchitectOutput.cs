using Newtonsoft.Json;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 副本建筑师AI输出
/// </summary>
public class DungeonArchitectOutput
{
    /// <summary>世界设定</summary>
    [JsonProperty("world_setting")]
    public WorldSettingData? WorldSetting { get; set; }

    /// <summary>NPC列表</summary>
    [JsonProperty("npcs")]
    public List<NpcData>? Npcs { get; set; }

    /// <summary>主线任务</summary>
    [JsonProperty("main_quest")]
    public MainQuestData? MainQuest { get; set; }

    /// <summary>支线任务</summary>
    [JsonProperty("side_quests")]
    public List<SideQuestData>? SideQuests { get; set; }

    /// <summary>隐藏内容</summary>
    [JsonProperty("hidden_content")]
    public List<HiddenContentData>? HiddenContent { get; set; }

    /// <summary>难度参数</summary>
    [JsonProperty("difficulty_params")]
    public DifficultyParamsData? DifficultyParams { get; set; }

    /// <summary>时间线</summary>
    [JsonProperty("timeline")]
    public List<TimelineEventData>? Timeline { get; set; }
}

/// <summary>
/// 世界设定
/// </summary>
public class WorldSettingData
{
    [JsonProperty("era")]
    public string Era { get; set; } = "";

    [JsonProperty("technology_level")]
    public string TechnologyLevel { get; set; } = "";

    [JsonProperty("culture")]
    public string Culture { get; set; } = "";

    [JsonProperty("geography")]
    public string Geography { get; set; } = "";

    [JsonProperty("key_locations")]
    public List<LocationData>? KeyLocations { get; set; }
}

/// <summary>
/// 地点数据
/// </summary>
public class LocationData
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("connections")]
    public List<string>? Connections { get; set; }
}

/// <summary>
/// NPC数据
/// </summary>
public class NpcData
{
    [JsonProperty("npc_id")]
    public string NpcId { get; set; } = "";

    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("role")]
    public string Role { get; set; } = "";

    [JsonProperty("personality")]
    public string Personality { get; set; } = "";

    [JsonProperty("language_style")]
    public string LanguageStyle { get; set; } = "";

    [JsonProperty("catchphrase")]
    public string Catchphrase { get; set; } = "";

    [JsonProperty("initial_attitude")]
    public int InitialAttitude { get; set; }

    [JsonProperty("motivation")]
    public string Motivation { get; set; } = "";

    [JsonProperty("location")]
    public string Location { get; set; } = "";

    [JsonProperty("action_plan")]
    public string ActionPlan { get; set; } = "";
}

/// <summary>
/// 主线任务数据
/// </summary>
public class MainQuestData
{
    [JsonProperty("objective")]
    public string Objective { get; set; } = "";

    [JsonProperty("key_nodes")]
    public List<string>? KeyNodes { get; set; }

    [JsonProperty("paths")]
    public List<QuestPathData>? Paths { get; set; }

    [JsonProperty("failure_conditions")]
    public List<string>? FailureConditions { get; set; }
}

/// <summary>
/// 路径数据
/// </summary>
public class QuestPathData
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    [JsonProperty("difficulty")]
    public string Difficulty { get; set; } = "";
}

/// <summary>
/// 支线任务数据
/// </summary>
public class SideQuestData
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    [JsonProperty("trigger")]
    public string Trigger { get; set; } = "";

    [JsonProperty("reward")]
    public string Reward { get; set; } = "";

    [JsonProperty("description")]
    public string Description { get; set; } = "";

    /// <summary>初始可见性: visible(立即可见) / hidden(条件触发)</summary>
    [JsonProperty("initial_visibility")]
    public string InitialVisibility { get; set; } = "visible";
}

/// <summary>
/// 隐藏内容数据
/// </summary>
public class HiddenContentData
{
    [JsonProperty("content")]
    public string Content { get; set; } = "";

    [JsonProperty("trigger_condition")]
    public string TriggerCondition { get; set; } = "";
}

/// <summary>
/// 难度参数数据
/// </summary>
public class DifficultyParamsData
{
    [JsonProperty("recommended_dc_range")]
    public string RecommendedDcRange { get; set; } = "";

    [JsonProperty("enemy_strength")]
    public string EnemyStrength { get; set; } = "";

    [JsonProperty("resource_scarcity")]
    public string ResourceScarcity { get; set; } = "";
}

/// <summary>
/// 时间线事件数据
/// </summary>
public class TimelineEventData
{
    [JsonProperty("day")]
    public int Day { get; set; }

    [JsonProperty("segment")]
    public string Segment { get; set; } = "";

    [JsonProperty("event")]
    public string Event { get; set; } = "";
}
