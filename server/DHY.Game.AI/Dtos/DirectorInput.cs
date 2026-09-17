namespace DHY.Game.AI.Dtos;

/// <summary>
/// 导演AI输入
/// </summary>
public class DirectorInput
{
    /// <summary>玩家行动</summary>
    public string PlayerAction { get; set; } = "";

    /// <summary>行动意图（由分类AI提炼，标准化格式："行动类别·动词：目标描述"）</summary>
    public string? ActionIntent { get; set; }

    /// <summary>世界状态JSON</summary>
    public string WorldState { get; set; } = "";

    /// <summary>副本设定</summary>
    public string DungeonContext { get; set; } = "";

    /// <summary>核心NPC列表</summary>
    public string NpcProfiles { get; set; } = "";

    /// <summary>主线进度</summary>
    public string MainQuestProgress { get; set; } = "";

    /// <summary>角色再定位片段(可null)</summary>
    public string? RepositionSnippet { get; set; }

    /// <summary>玩家背包摘要（每次交互动态生成）</summary>
    public string PlayerInventory { get; set; } = "";

    /// <summary>玩家角色名称</summary>
    public string CharacterName { get; set; } = "";

    /// <summary>判定结果文本（由代码层掷骰后生成，null表示本次无需检定）</summary>
    public string? JudgmentOutcome { get; set; }

    /// <summary>剧情是否停滞（连续多轮无实质推进时为true，提示导演AI主动引导）</summary>
    public bool IsStagnant { get; set; }

    /// <summary>支线任务清单（由建筑师AI生成，含触发条件，供导演埋线索/制造触发契机；完成标记归书记官）</summary>
    public string SideQuestList { get; set; } = "";

    /// <summary>隐藏内容清单（由建筑师AI生成，含触发条件，供导演埋线索；发现标记归书记官）</summary>
    public string HiddenContentList { get; set; } = "";

    /// <summary>
    /// 是否为推进型行动（玩家点选了粗粒度剧情推进选项）。
    /// true 时注入[推进型行动]标记，导演必须强制 beat_scale=chapter 并输出 beats 分镜，
    /// 一次性推演完整弧线而非只推演第一步。
    /// </summary>
    public bool IsAdvanceAction { get; set; }
}
