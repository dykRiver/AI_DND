using Newtonsoft.Json;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 玩家辅导员AI输入。
/// 与书记官同源：读取「世界状态快照(本轮开始前) + 导演既定事实」即可掌握本轮完整世界图景，
/// 无需等书记官落库（书记官的 world_state_changes 只是导演既定事实的结构化编码，不产生新信息）。
/// </summary>
public class CoachInput
{
    /// <summary>玩家原始目标文本（自由输入框提交的中长期意图，非本轮行动）</summary>
    public string PlayerGoal { get; set; } = "";

    /// <summary>上轮持久化的辅导员状态JSON（首轮为null，用于滚动评估进度）</summary>
    public string? PrevCoachState { get; set; }

    /// <summary>世界状态快照JSON（本轮开始前，含历史）</summary>
    public string WorldState { get; set; } = "";

    /// <summary>本轮导演输出的既定事实文本（本轮世界增量，辅导员据此评估进度）</summary>
    public string DirectorFacts { get; set; } = "";

    /// <summary>核心NPC列表</summary>
    public string NpcProfiles { get; set; } = "";

    /// <summary>玩家背包摘要</summary>
    public string PlayerInventory { get; set; } = "";

    /// <summary>主线进度</summary>
    public string MainQuestProgress { get; set; } = "";

    /// <summary>玩家角色名称</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>是否成人轮（决定模板/模型：Coach / AdultCoach）</summary>
    public bool IsAdult { get; set; }
}

/// <summary>
/// 玩家辅导员AI输出（严格JSON，snake_case）。
/// </summary>
public class CoachOutput
{
    /// <summary>消歧后的清晰目标（首轮确立，后续微调）</summary>
    [JsonProperty("clarified_intent")]
    public string? ClarifiedIntent { get; set; }

    /// <summary>目标状态：active / complete / failed</summary>
    [JsonProperty("goal_status")]
    public string? GoalStatus { get; set; }

    /// <summary>本轮世界状态下，目标推进到哪一步、已做了什么、还差什么</summary>
    [JsonProperty("progress_note")]
    public string? ProgressNote { get; set; }

    /// <summary>滚动更新后的当前子目标（眼下最该推进的一件事）</summary>
    [JsonProperty("current_subgoal")]
    public string? CurrentSubgoal { get; set; }

    /// <summary>下一步策略方向（1-2个，由辅导员自决数量），注入书记官引导选项生成</summary>
    [JsonProperty("next_focus")]
    public List<NextFocusItem>? NextFocus { get; set; }

    /// <summary>目标涉及的关键实体（人物/物品/地点）</summary>
    [JsonProperty("entities_involved")]
    public List<string>? EntitiesInvolved { get; set; }

    /// <summary>goal_status=complete 时的达成依据；否则空</summary>
    [JsonProperty("completion_check")]
    public string? CompletionCheck { get; set; }
}

/// <summary>
/// 下一步策略方向条目
/// </summary>
public class NextFocusItem
{
    /// <summary>策略方向（扎根当前世界状态、服务目标，可被书记官转化为具体选项）</summary>
    [JsonProperty("direction")]
    public string Direction { get; set; } = "";

    /// <summary>推演依据（为何这是当前最优方向）</summary>
    [JsonProperty("rationale")]
    public string Rationale { get; set; } = "";
}

/// <summary>
/// 玩家辅导员持久化状态（序列化后存 GameDungeonSession.CoachState）。
/// 真实轮辅导员Full跑后覆盖写入；预计算DryRun读取复用其 next_focus、不写。
/// </summary>
public class CoachState
{
    /// <summary>消歧后的清晰目标</summary>
    [JsonProperty("clarified_intent")]
    public string? ClarifiedIntent { get; set; }

    /// <summary>当前子目标</summary>
    [JsonProperty("current_subgoal")]
    public string? CurrentSubgoal { get; set; }

    /// <summary>进度描述</summary>
    [JsonProperty("progress_note")]
    public string? ProgressNote { get; set; }

    /// <summary>目标状态：active / complete / failed</summary>
    [JsonProperty("goal_status")]
    public string GoalStatus { get; set; } = CoachGoalStatus.Active;

    /// <summary>下一步策略方向（供预计算DryRun复用注入书记官）</summary>
    [JsonProperty("next_focus")]
    public List<NextFocusItem>? NextFocus { get; set; }

    /// <summary>目标涉及的关键实体</summary>
    [JsonProperty("entities_involved")]
    public List<string>? EntitiesInvolved { get; set; }

    /// <summary>更新时的交互轮次</summary>
    [JsonProperty("updated_round")]
    public int UpdatedRound { get; set; }

    /// <summary>
    /// 目标起始轮次（首次被辅导员评估的 applyRound，冷启动时写入、后续轮透传）。
    /// 供代码兜底判定目标已持续轮数 = UpdatedRound - GoalStartRound + 1。
    /// </summary>
    [JsonProperty("goal_start_round")]
    public int GoalStartRound { get; set; }
}

/// <summary>
/// 玩家目标状态常量
/// </summary>
public static class CoachGoalStatus
{
    /// <summary>进行中（默认）</summary>
    public const string Active = "active";

    /// <summary>已达成</summary>
    public const string Complete = "complete";

    /// <summary>已失效（不可达）</summary>
    public const string Failed = "failed";

    /// <summary>已超期（代码兜底：持续超过 MaxCoachGoalRounds 轮仍未达成而强制归档）</summary>
    public const string Expired = "expired";
}

/// <summary>
/// 目标终结事件（辅导员判定 complete/failed 时由协调器回填 GameActionResult，供 Hub 推送 + 清空目标）。
/// completion_check 达成依据经此事件直传前端，不持久化到 CoachState（见设计 §9 / ℹ️-C）。
/// </summary>
public class CoachGoalEvent
{
    /// <summary>终结状态：complete（达成）/ failed（失效不可达）</summary>
    public string Status { get; set; } = "";

    /// <summary>消歧后的清晰目标（供推送文案回显“达成了什么”）</summary>
    public string? ClarifiedIntent { get; set; }

    /// <summary>complete：达成依据(completion_check)；failed：失效说明(progress_note)</summary>
    public string? Note { get; set; }
}
