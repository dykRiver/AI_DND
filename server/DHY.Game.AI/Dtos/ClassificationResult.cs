namespace DHY.Game.AI.Dtos;

/// <summary>
/// 行动分类结果
/// </summary>
public class ClassificationResult
{
    /// <summary>是否为常规行动</summary>
    public bool IsRoutine { get; set; }

    /// <summary>置信度 (0.0-1.0)</summary>
    public double Confidence { get; set; }

    /// <summary>原因</summary>
    public string? Reason { get; set; }

    /// <summary>行动是否可行（分类AI判定，默认true）</summary>
    public bool IsFeasible { get; set; } = true;

    /// <summary>不可行原因（仅当IsFeasible=false时由AI输出）</summary>
    public string? InfeasibleReason { get; set; }

    /// <summary>是否需要状态变更（搜索/拾取/移动等需要更新世界状态或获取道具时为true）</summary>
    public bool NeedsStateChange { get; set; }

    /// <summary>是否为成人色情内容（跳过导演AI，直接叙事）</summary>
    public bool IsAdult { get; set; }

    /// <summary>行动意图提炼（标准化格式："行动类别·动词：目标描述"）</summary>
    public string? ActionIntent { get; set; }

    /// <summary>判定信息（非常规行动时由分类AI输出，常规行动时为null）</summary>
    public JudgmentInfo? Judgment { get; set; }
}
