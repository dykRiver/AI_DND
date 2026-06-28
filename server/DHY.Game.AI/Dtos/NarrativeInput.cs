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

    /// <summary>场景类型（用于字数控制）</summary>
    public string SceneType { get; set; } = "daily";

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
