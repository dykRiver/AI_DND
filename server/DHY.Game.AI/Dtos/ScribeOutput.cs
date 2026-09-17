using DHY.Game.Core.Dtos;
using Newtonsoft.Json;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 书记官AI输出（状态记账执行器：依前台导演的既定事实，产出全部结构化状态字段）
/// </summary>
public class ScribeOutput
{
    /// <summary>世界状态变更（结构化，代码层合并到局面快照）</summary>
    [JsonProperty("world_state_changes")]
    public WorldStateChangesDto? WorldStateChanges { get; set; }

    /// <summary>
    /// 物资清单（权威事实基准）：本轮玩家获得/消耗/失去的关键道具或情报，
    /// 由物资官(道具AI)逐条扩展为完整数值后落库。
    /// </summary>
    [JsonProperty("item_hints")]
    public List<ItemHintInfo>? ItemHints { get; set; }

    /// <summary>NPC态度变化列表（从前台导演的npc_actions中剥离，由书记官依既定事实判定）</summary>
    [JsonProperty("npc_attitude_changes")]
    public List<NpcAttitudeChangeInfo>? NpcAttitudeChanges { get; set; }

    /// <summary>建议行动选项（每次输出恰好2个，供玩家快速选择与下一轮预计算）</summary>
    [JsonProperty("suggested_actions")]
    public List<SuggestedActionInfo>? SuggestedActions { get; set; }
}

/// <summary>
/// NPC态度变化条目
/// </summary>
public class NpcAttitudeChangeInfo
{
    /// <summary>NPC标识</summary>
    [JsonProperty("npc_id")]
    public string NpcId { get; set; } = "";

    /// <summary>态度变化值（负=恶化，正=改善，0不输出）</summary>
    [JsonProperty("change")]
    public int Change { get; set; }
}

/// <summary>
/// 书记官AI输入
/// </summary>
public class ScribeInput
{
    /// <summary>玩家行动</summary>
    public string PlayerAction { get; set; } = "";

    /// <summary>行动意图（由分类AI提炼）</summary>
    public string? ActionIntent { get; set; }

    /// <summary>判定结果文本（null表示本次无检定）</summary>
    public string? JudgmentOutcome { get; set; }

    /// <summary>前台导演输出的既定事实文本（叙事种子/NPC行动/钩子等，书记官只记录不改写）</summary>
    public string DirectorFacts { get; set; } = "";

    /// <summary>世界状态JSON</summary>
    public string WorldState { get; set; } = "";

    /// <summary>核心NPC列表</summary>
    public string NpcProfiles { get; set; } = "";

    /// <summary>玩家背包摘要</summary>
    public string PlayerInventory { get; set; } = "";

    /// <summary>主线进度</summary>
    public string MainQuestProgress { get; set; } = "";

    /// <summary>支线任务清单（标记完成时精确匹配任务名）</summary>
    public string SideQuestList { get; set; } = "";

    /// <summary>隐藏内容清单（标记发现时精确匹配内容名）</summary>
    public string HiddenContentList { get; set; } = "";

    /// <summary>玩家角色名称</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>仅产出建议选项（纯叙事轮轻量模式：只输出suggested_actions，跳过状态记账与落库）</summary>
    public bool SuggestionsOnly { get; set; }

    /// <summary>
    /// 选项节奏档位：true=关键时刻（导演判定抉择点/章节档/高紧张），输出细粒度慢节奏选项；
    /// false=非关键时刻，输出粗粒度剧情推进型选项（选中后可驱动导演大幅推演剧情）。
    /// </summary>
    public bool IsKeyMoment { get; set; } = true;
}
