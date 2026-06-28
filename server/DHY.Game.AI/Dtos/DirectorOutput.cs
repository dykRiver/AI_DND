using DHY.Game.Core.Dtos;
using Newtonsoft.Json;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 导演AI输出
/// </summary>
public class DirectorOutput
{
    /// <summary>叙事方向</summary>
    [JsonProperty("narrative_direction")]
    public string NarrativeDirection { get; set; } = "";

    /// <summary>NPC行为列表</summary>
    [JsonProperty("npc_actions")]
    public List<NpcActionInfo>? NpcActions { get; set; }

    /// <summary>世界状态变更（结构化，代码层合并到局面快照）</summary>
    [JsonProperty("world_state_changes")]
    public WorldStateChangesDto? WorldStateChanges { get; set; }

    /// <summary>节奏信息</summary>
    [JsonProperty("pacing")]
    public PacingInfo? Pacing { get; set; }

    /// <summary>感官提示</summary>
    [JsonProperty("sensory_hint")]
    public string? SensoryHint { get; set; }

    /// <summary>玩家选择点</summary>
    [JsonProperty("player_choice_point")]
    public bool PlayerChoicePoint { get; set; }

    /// <summary>是否推进时段（玩家行动消耗了大量时间时由AI设为true）</summary>
    [JsonProperty("time_advance")]
    public bool TimeAdvance { get; set; }

    /// <summary>本次行动获得的道具列表（玩家拾取/获得道具时由AI输出）</summary>
    [JsonProperty("acquired_items")]
    public List<AcquiredItemInfo>? AcquiredItems { get; set; }

    /// <summary>本次行动消耗的道具列表（玩家使用消耗品/弹药时由AI输出）</summary>
    [JsonProperty("consumed_items")]
    public List<ConsumedItemInfo>? ConsumedItems { get; set; }

    /// <summary>叙事钩子（导演AI主动提供的引导线索，供叙事AI融入文本）</summary>
    [JsonProperty("narrative_hooks")]
    public List<string>? NarrativeHooks { get; set; }
}

/// <summary>
/// NPC行为信息
/// </summary>
public class NpcActionInfo
{
    /// <summary>NPC标识</summary>
    [JsonProperty("npc_id")]
    public string NpcId { get; set; } = "";

    /// <summary>行为描述</summary>
    [JsonProperty("action")]
    public string Action { get; set; } = "";

    /// <summary>台词大意</summary>
    [JsonProperty("dialogue_gist")]
    public string? DialogueGist { get; set; }

    /// <summary>态度变化</summary>
    [JsonProperty("attitude_change")]
    public int AttitudeChange { get; set; }
}

/// <summary>
/// 节奏信息
/// </summary>
public class PacingInfo
{
    /// <summary>紧张度 (1-10)</summary>
    [JsonProperty("tension_level")]
    public int TensionLevel { get; set; }

    /// <summary>节奏说明</summary>
    [JsonProperty("note")]
    public string? Note { get; set; }
}

/// <summary>
/// AI声明消耗的道具信息
/// </summary>
public class ConsumedItemInfo
{
    /// <summary>道具名称（精确匹配背包中同名道具）</summary>
    [JsonProperty("item_name")]
    public string ItemName { get; set; } = "";

    /// <summary>消耗数量（默认1）</summary>
    [JsonProperty("quantity")]
    public int Quantity { get; set; } = 1;

    /// <summary>消耗原因（调试日志用）</summary>
    [JsonProperty("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// AI生成的道具信息（玩家获得道具时输出）
/// </summary>
public class AcquiredItemInfo
{
    /// <summary>道具名称</summary>
    [JsonProperty("item_name")]
    public string ItemName { get; set; } = "";

    /// <summary>道具类型 (武器/防具/消耗品/关键道具/杂物)</summary>
    [JsonProperty("item_type")]
    public string ItemType { get; set; } = "杂物";

    /// <summary>道具描述</summary>
    [JsonProperty("description")]
    public string? Description { get; set; }

    /// <summary>重量单位（支持1位小数，四舍五入到0.1）</summary>
    [JsonProperty("weight")]
    public decimal Weight { get; set; }

    /// <summary>属性加值（装备时生效）</summary>
    [JsonProperty("attribute_bonus")]
    public int AttributeBonus { get; set; }

    /// <summary>关联属性 (STR/DEX/CON/INT/WIS/CHA)</summary>
    [JsonProperty("linked_attribute")]
    public string? LinkedAttribute { get; set; }

    /// <summary>最大使用次数（0=无限）</summary>
    [JsonProperty("max_uses")]
    public int MaxUses { get; set; }

    /// <summary>是否无限使用</summary>
    [JsonProperty("is_unlimited")]
    public bool IsUnlimited { get; set; }
}
