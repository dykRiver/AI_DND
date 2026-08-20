namespace DHY.Game.AIEval.Model;

/// <summary>案例公共字段（snake_case：id/desc）</summary>
public abstract class EvalCaseBase
{
    public string Id { get; set; } = "";
    public string Desc { get; set; } = "";
}

// ─────────────────────────── classifier ───────────────────────────

public class ClassifierCase : EvalCaseBase
{
    /// <summary>局面快照文本（场景/位置/环境/NPC状态）</summary>
    public string Scenario { get; set; } = "";

    /// <summary>玩家背包摘要（资产账本，空=空背包）</summary>
    public string Inventory { get; set; } = "";

    /// <summary>NPC档案摘要</summary>
    public string NpcProfiles { get; set; } = "";

    /// <summary>玩家输入</summary>
    public string PlayerInput { get; set; } = "";

    public ClassifierExpect Expect { get; set; } = new();
}

public class ClassifierExpect
{
    /// <summary>期望可行性三态（feasible/uncertain/infeasible，精确匹配）</summary>
    public string? Feasibility { get; set; }

    /// <summary>期望是否常规行动</summary>
    public bool? IsRoutine { get; set; }

    /// <summary>期望是否成人内容</summary>
    public bool? IsAdult { get; set; }

    /// <summary>期望是否需要检定</summary>
    public bool? JudgmentNeeded { get; set; }

    /// <summary>DC合理区间 [min, max]（需要检定时校验）</summary>
    public int[]? DcRange { get; set; }

    /// <summary>期望技能名包含的子串（如"隐匿"）</summary>
    public string? SkillHint { get; set; }
}

// ─────────────────────────── director ───────────────────────────

public class DirectorCase : EvalCaseBase
{
    public DirectorInputCase Input { get; set; } = new();
    public DirectorExpect Expect { get; set; } = new();
}

/// <summary>导演输入（字段与 DirectorInput 对应，snake_case）</summary>
public class DirectorInputCase
{
    public string PlayerAction { get; set; } = "";
    public string? ActionIntent { get; set; }
    public string WorldState { get; set; } = "";
    public string DungeonContext { get; set; } = "";
    public string NpcProfiles { get; set; } = "";
    public string MainQuestProgress { get; set; } = "";
    public string PlayerInventory { get; set; } = "";
    public bool IsRoutine { get; set; }
    public string CharacterName { get; set; } = "";
    /// <summary>判定结果文本（null=无需检定）</summary>
    public string? JudgmentOutcome { get; set; }
    public bool NeedsStateChange { get; set; }
    public string SideQuestList { get; set; } = "";
    public string HiddenContentList { get; set; } = "";
}

public class DirectorExpect
{
    /// <summary>judge层：本用例判定结果语义（true=成功/false=失败），用于审查叙事种子与成败一致性</summary>
    public bool? JudgmentSuccess { get; set; }

    /// <summary>item_hints 应包含的名称（包含匹配）</summary>
    public List<string>? ExpectHintNames { get; set; }

    /// <summary>item_hints 不得出现的名称（hint纪律：普通物品不应进账本）</summary>
    public List<string>? ForbidHintNames { get; set; }

    /// <summary>期望节拍分档（可选，精确匹配）</summary>
    public string? ExpectBeatScale { get; set; }

    /// <summary>期望输出 NPC 对话指导（社交场景用例设 true）</summary>
    public bool? ExpectDialogue { get; set; }
}

// ─────────────────────────── quartermaster ───────────────────────────

public class QuartermasterCase : EvalCaseBase
{
    /// <summary>本轮玩家行动</summary>
    public string PlayerAction { get; set; } = "";

    /// <summary>导演蓝图 item_hints 的文本形式（模拟导演输出给物资官的原文）</summary>
    public string ItemHintsText { get; set; } = "";

    /// <summary>结构化蓝图（传给服务做失败保底；正常路径同样携带）</summary>
    public List<ItemHintInfo> Blueprint { get; set; } = new();

    /// <summary>当前账本文本</summary>
    public string CurrentLedger { get; set; } = "";

    public QuartermasterExpect Expect { get; set; } = new();
}

public class QuartermasterExpect
{
    /// <summary>期望登记的获得物理道具名（包含匹配，顺序无关）</summary>
    public List<string>? AcquiredItemNames { get; set; }

    /// <summary>期望登记的消耗物理道具名</summary>
    public List<string>? ConsumedItemNames { get; set; }

    /// <summary>期望登记的遗失物理道具名</summary>
    public List<string>? LostItemNames { get; set; }

    /// <summary>期望登记的获得情报名</summary>
    public List<string>? AcquiredInfoNames { get; set; }

    /// <summary>期望登记的失效情报名</summary>
    public List<string>? InvalidatedInfoNames { get; set; }

    /// <summary>禁止蓝图外新增条目（道具纪律：不丢不多）</summary>
    public bool ForbidExtra { get; set; } = true;

    /// <summary>获得物理道具必须补全数值字段（weight>0、类型非空）</summary>
    public bool RequireItemNumeric { get; set; } = true;

    /// <summary>情报不得混入物理道具分组</summary>
    public bool InfoNotAsItem { get; set; } = true;
}

// ─────────────────────────── narrative ───────────────────────────

public class NarrativeCase : EvalCaseBase
{
    public NarrativeInputCase Input { get; set; } = new();
    public NarrativeExpect Expect { get; set; } = new();
}

public class NarrativeInputCase
{
    /// <summary>场景类型（explore/dialogue/combat/horror/daily）</summary>
    public string SceneType { get; set; } = "explore";

    /// <summary>本轮目标字数</summary>
    public int WordTarget { get; set; } = 600;

    /// <summary>玩家角色名（保真检查目标）</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>文风圣经（含禁用陈词）</summary>
    public string StyleBible { get; set; } = "";

    /// <summary>意象追踪</summary>
    public string MotifTracker { get; set; } = "";

    /// <summary>世界上下文摘要</summary>
    public string WorldContext { get; set; } = "";

    /// <summary>玩家装备摘要</summary>
    public string PlayerInventory { get; set; } = "";

    /// <summary>最近叙事（文风连贯参考，可为空）</summary>
    public string RecentNarrative { get; set; } = "";

    /// <summary>NPC语言卡片</summary>
    public List<NpcLanguageCardDto> NpcLanguageCards { get; set; } = new();

    /// <summary>导演蓝图（叙事种子/文风指导/对话指导等）</summary>
    public DirectorOutput Blueprint { get; set; } = new();
}

public class NarrativeExpect
{
    /// <summary>字数区间 [min, max]</summary>
    public int[]? WordRange { get; set; }

    /// <summary>禁用陈词（出现即失败）</summary>
    public List<string>? ForbiddenWords { get; set; }

    /// <summary>必须出现的文本（如玩家角色名，保真检查）</summary>
    public List<string>? MustMention { get; set; }

    /// <summary>禁止泄露的事实关键词（信息泄露检查）</summary>
    public List<string>? ForbiddenFacts { get; set; }

    /// <summary>是否启用 LLM 评审（文风/一致性/感官节奏）</summary>
    public bool Judge { get; set; } = true;
}
