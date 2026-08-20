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

    /// <summary>文风圣经（一次性生成的文风档案，每轮注入叙事AI）</summary>
    [JsonProperty("style_bible")]
    public StyleBibleData? StyleBible { get; set; }

    /// <summary>贯穿意象列表（3-5个，在副本中反复出现并进化）</summary>
    [JsonProperty("motifs")]
    public List<MotifData>? Motifs { get; set; }
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

/// <summary>
/// 文风圣经数据（由建筑师AI一次性生成，存储于session，每轮注入叙事AI）
/// </summary>
public class StyleBibleData
{
    /// <summary>语调（如"阴冷克制的现实主义，偶有诗意的恐怖意象"）</summary>
    [JsonProperty("tone")]
    public string Tone { get; set; } = "";

    /// <summary>句式偏好（如"短句堆叠制造窒息感；环境描写用绵长从句"）</summary>
    [JsonProperty("sentence_rhythm")]
    public string SentenceRhythm { get; set; } = "";

    /// <summary>感官调色板（如["消毒水味","荧光灯喗鸣","瓷砖冷意","远处轮子吱呀声"]）</summary>
    [JsonProperty("sensory_palette")]
    public List<string>? SensoryPalette { get; set; }

    /// <summary>禁用陈词列表（如["令人毛骨悚然","不寒而栗","脊背发凉"]）</summary>
    [JsonProperty("forbidden_cliches")]
    public List<string>? ForbiddenCliches { get; set; }
}

/// <summary>
/// 意象数据（由建筑师AI一次性生成，在副本中反复出现并进化）
/// </summary>
public class MotifData
{
    /// <summary>意象名称（如"滴水声"）</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    /// <summary>初始状态描述（如"走廊尽头的天花板在漏水，滴答声在空旷的空间里回荡"）</summary>
    [JsonProperty("initial_state")]
    public string InitialState { get; set; } = "";

    /// <summary>进化方向提示（如"从背景噪音→战斗中的节奏干扰→揭示为血水"）</summary>
    [JsonProperty("evolution_hint")]
    public string EvolutionHint { get; set; } = "";
}
