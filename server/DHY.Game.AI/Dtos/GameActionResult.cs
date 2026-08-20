using DHY.Game.Core.Dtos;
using DHY.Game.Core.Entities;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 游戏行动结果
/// </summary>
public class GameActionResult
{
    /// <summary>叙事文本</summary>
    public string Narrative { get; set; } = "";

    /// <summary>叙事输入(Hub流式生成用，非空时由Hub调用叙事AI流式推送)</summary>
    public NarrativeInput? NarrativeInput { get; set; }

    /// <summary>骰子判定结果(若有)</summary>
    public GameDiceRollRecord? DiceResult { get; set; }

    /// <summary>状态变更</summary>
    public GameStateUpdate? StateChanges { get; set; }

    /// <summary>是否为选择点</summary>
    public bool IsChoicePoint { get; set; }

    /// <summary>导演AI建议的行动选项（供前端显示快速选择按钮）</summary>
    public List<SuggestedActionInfo>? SuggestedActions { get; set; }

    /// <summary>分类AI判定是否需要状态变更（缓存命中回放时用于守卫持久化）</summary>
    public bool NeedsStateChange { get; set; }

    /// <summary>导演蓝图物资清单（权威事实基准，供物资官逐条记账落库）</summary>
    public List<ItemHintInfo>? ItemHints { get; set; }

    /// <summary>分类AI提炼的行动意图（供物资官记账入参与日志）</summary>
    public string? ActionIntent { get; set; }

    /// <summary>可行性三态：feasible / uncertain / infeasible（供三态分流：infeasible 短路拒绝）</summary>
    public string? Feasibility { get; set; }
}

/// <summary>
/// 游戏状态变更
/// </summary>
public class GameStateUpdate
{
    /// <summary>HP变化</summary>
    public int? HpChange { get; set; }

    /// <summary>新HP值</summary>
    public int? NewHp { get; set; }

    /// <summary>时间是否推进</summary>
    public bool? TimeAdvanced { get; set; }

    /// <summary>NPC态度变化</summary>
    public Dictionary<string, int>? NpcAttitudeChanges { get; set; }

    /// <summary>新获得物品</summary>
    public List<string>? NewItems { get; set; }

    /// <summary>任务进度更新（导演AI输出quest_progress时非null）</summary>
    public QuestProgressDto? QuestProgress { get; set; }
}
