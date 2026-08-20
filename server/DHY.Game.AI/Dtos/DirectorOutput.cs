using DHY.Game.Core.Dtos;
using Newtonsoft.Json;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 导演AI输出
/// </summary>
public class DirectorOutput
{
    /// <summary>叙事种子（文学性段落，直接传递叙事氛围和画面感，250字内）</summary>
    [JsonProperty("narrative_seed")]
    public string NarrativeSeed { get; set; } = "";

    /// <summary>
    /// 节拍分档（由导演判定本拍的剧情分量，驱动叙事字数与渲染方式）：
    /// micro=普通对话/观察/日常；normal=探索推进/遭遇/支线小节点；chapter=重大决策/主线节点/战斗高潮/剧情转折。
    /// 未输出时代码层回退到场景类型默认逻辑。
    /// </summary>
    [JsonProperty("beat_scale")]
    public string BeatScale { get; set; } = "";

    /// <summary>
    /// 章节分镜表（仅 beat_scale=chapter 时输出）：把这一章拆成若干有序子节拍，
    /// 叙事AI按分镜逐段续写、拼成整章。非章节档时为 null。
    /// </summary>
    [JsonProperty("beats")]
    public List<ChapterBeatInfo>? Beats { get; set; }

    /// <summary>文风指导（句式节奏+感官重点+文学手法，指导叙事AI的文风选择）</summary>
    [JsonProperty("prose_guidance")]
    public string ProseGuidance { get; set; } = "";

    /// <summary>NPC行为列表</summary>
    [JsonProperty("npc_actions")]
    public List<NpcActionInfo>? NpcActions { get; set; }

    /// <summary>世界状态变更（结构化，代码层合并到局面快照）</summary>
    [JsonProperty("world_state_changes")]
    public WorldStateChangesDto? WorldStateChanges { get; set; }

    /// <summary>节奏信息</summary>
    [JsonProperty("pacing")]
    public PacingInfo? Pacing { get; set; }

    /// <summary>本轮叙事目标字数（导演依据这一拍的实际内容判定，代码层clamp到安全区间后驱动叙事字数与校验）</summary>
    [JsonProperty("narrative_word_target")]
    public int NarrativeWordTarget { get; set; }

    /// <summary>玩家选择点</summary>
    [JsonProperty("player_choice_point")]
    public bool PlayerChoicePoint { get; set; }

    /// <summary>是否推进时段（玩家行动消耗了大量时间时由AI设为true）</summary>
    [JsonProperty("time_advance")]
    public bool TimeAdvance { get; set; }

    /// <summary>
    /// 物资清单（权威事实基准）：导演判定本轮玩家获得/消耗/失去的关键道具或情报，
    /// 由物资官(道具AI)逐条扩展为完整数值后落库。仅 hint 关键剧情道具/重要资产，普通资源不 hint。
    /// </summary>
    [JsonProperty("item_hints")]
    public List<ItemHintInfo>? ItemHints { get; set; }

    /// <summary>[已弃用] 旧版权威获得道具字段，仅保留反序列化容错，代码层不再落库</summary>
    [JsonProperty("acquired_items")]
    public List<AcquiredItemInfo>? AcquiredItems { get; set; }

    /// <summary>[已弃用] 旧版权威消耗道具字段，仅保留反序列化容错，代码层不再落库</summary>
    [JsonProperty("consumed_items")]
    public List<ConsumedItemInfo>? ConsumedItems { get; set; }

    /// <summary>叙事钩子（导演AI主动提供的引导线索，供叙事AI融入文本）</summary>
    [JsonProperty("narrative_hooks")]
    public List<string>? NarrativeHooks { get; set; }

    /// <summary>建议行动选项（导演AI每次推演输出2个，供玩家快速选择）</summary>
    [JsonProperty("suggested_actions")]
    public List<SuggestedActionInfo>? SuggestedActions { get; set; }
}

/// <summary>
/// 物资清单条目（导演给物资官的权威蓝图：名称+类别+变更方向+简述+是否关键）
/// </summary>
public class ItemHintInfo
{
    /// <summary>名称（道具名或情报标识）</summary>
    [JsonProperty("name")]
    public string Name { get; set; } = "";

    /// <summary>类别：物品 / 情报</summary>
    [JsonProperty("category")]
    public string Category { get; set; } = "物品";

    /// <summary>变更方向：获得 / 消耗 / 失去</summary>
    [JsonProperty("change")]
    public string Change { get; set; } = "获得";

    /// <summary>简述（为何涉及、大致性质，供物资官补全数值参考）</summary>
    [JsonProperty("note")]
    public string? Note { get; set; }

    /// <summary>是否关键剧情道具/重要资产（导演仅应 hint 关键项，此标记供落库与门卫区分关键/普通）</summary>
    [JsonProperty("is_key")]
    public bool IsKey { get; set; }
}

/// <summary>
/// 章节分镜子节拍（导演在 chapter 档输出，供叙事AI分段续写）
/// </summary>
public class ChapterBeatInfo
{
    /// <summary>本段迷你场景种子（文学性速写，供叙事AI在此基础上扩写本段）</summary>
    [JsonProperty("seed")]
    public string Seed { get; set; } = "";

    /// <summary>本段类型（铺陈/冲突/对话/高潮/转折/收尾等）</summary>
    [JsonProperty("beat_type")]
    public string BeatType { get; set; } = "";

    /// <summary>本段焦点（应重点渲染的感官或情绪落点）</summary>
    [JsonProperty("focus")]
    public string Focus { get; set; } = "";
}

/// <summary>
/// 建议行动选项（导演AI输出，供玩家快速选择）
/// </summary>
public class SuggestedActionInfo
{
    /// <summary>行动文本（玩家可执行的行动描述，15字内）</summary>
    [JsonProperty("action_text")]
    public string ActionText { get; set; } = "";

    /// <summary>方向提示（如"社交互动方向"、"潜行探索方向"）</summary>
    [JsonProperty("hint")]
    public string Hint { get; set; } = "";
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

    /// <summary>对话指导（含表层/潜台词/隐瞒/身体语言，供叙事AI写出有张力的对话场景）</summary>
    [JsonProperty("dialogue_direction")]
    public NpcDialogueDirection? DialogueDirection { get; set; }

    /// <summary>态度变化</summary>
    [JsonProperty("attitude_change")]
    public int AttitudeChange { get; set; }
}

/// <summary>
/// NPC对话指导（多层结构，供叙事AI写出有深度的对话）
/// </summary>
public class NpcDialogueDirection
{
    /// <summary>表层：NPC实际说的话（大意）</summary>
    [JsonProperty("surface")]
    public string Surface { get; set; } = "";

    /// <summary>潜台词：NPC真正想表达的意思或动机</summary>
    [JsonProperty("subtext")]
    public string Subtext { get; set; } = "";

    /// <summary>隐瞒：NPC有意隐藏的信息（无隐瞒时留空）</summary>
    [JsonProperty("conceal")]
    public string Conceal { get; set; } = "";

    /// <summary>身体语言：暗示真实情绪的动作/表情/姿态</summary>
    [JsonProperty("body_language")]
    public string BodyLanguage { get; set; } = "";
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
