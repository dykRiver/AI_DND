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

    /// <summary>本次获得的道具列表</summary>
    public List<GameInventoryItem>? AcquiredItems { get; set; }

    /// <summary>本次是否有道具被消耗（用于Hub判断是否推送背包更新）</summary>
    public bool HasConsumedItems { get; set; }
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
