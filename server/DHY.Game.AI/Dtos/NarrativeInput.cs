using DHY.Game.Core.Entities;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 叙事AI输入
/// </summary>
public class NarrativeInput
{
    /// <summary>导演蓝图</summary>
    public DirectorOutput DirectorBlueprint { get; set; } = new();

    /// <summary>NPC语言卡片</summary>
    public List<NpcLanguageCardDto> NpcLanguageCards { get; set; } = new();

    /// <summary>最近叙事文本（保持文风连贯）</summary>
    public string RecentNarrative { get; set; } = "";

    /// <summary>判定结果(若有)</summary>
    public GameDiceRollRecord? JudgmentResult { get; set; }

    /// <summary>场景类型（用于文风模块选择）</summary>
    public string SceneType { get; set; } = "daily";

    /// <summary>本轮叙事目标字数（由导演给出、代码层clamp后传入，动态注入提示词与字数校验）</summary>
    public int WordTarget { get; set; }

    /// <summary>世界上下文摘要（世界设定+当前状态，供叙事AI维持世界观一致性）</summary>
    public string WorldContext { get; set; } = "";

    /// <summary>玩家当前装备与道具摘要（供叙事AI自然提及装备细节）</summary>
    public string PlayerInventory { get; set; } = "";

    /// <summary>玩家角色名称（供叙事AI在NPC对话等场景中正确称呼玩家）</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>是否为成人内容叙事（跳过导演AI，使用独立提示词）</summary>
    public bool IsAdult { get; set; }

    /// <summary>玩家行动文本（成人内容时直接传递给叙事AI，替代导演蓝图）</summary>
    public string PlayerAction { get; set; } = "";

    /// <summary>文风圣经（建筑师AI一次性生成，每轮注入，提供语调/句式/感官调色板/禁用陈词）</summary>
    public string StyleBible { get; set; } = "";

    /// <summary>意象进化追踪（建筑师AI生成的意象及其演变状态，每轮注入）</summary>
    public string MotifTracker { get; set; } = "";

    /// <summary>场景类型对应的文风模块（由代码层根据SceneType生成，指导句式/节奏/感官/文学手法）</summary>
    public string SceneStyleModule { get; set; } = "";
}

/// <summary>
/// NPC语言卡片DTO (AI层使用)
/// </summary>
public class NpcLanguageCardDto
{
    /// <summary>NPC名称</summary>
    public string NpcName { get; set; } = "";

    /// <summary>语言风格</summary>
    public string LanguageStyle { get; set; } = "";

    /// <summary>口头禅</summary>
    public string Catchphrase { get; set; } = "";

    /// <summary>当前态度</summary>
    public int CurrentAttitude { get; set; }
}
