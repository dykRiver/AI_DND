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

    /// <summary>
    /// 可行性三态：feasible（可行）/ uncertain（存疑，引用了疑似曾获得但账本查无，跑导演叙事终审）/ infeasible（凭空且无依据，短路拒绝）。
    /// 默认 feasible。
    /// </summary>
    public string Feasibility { get; set; } = "feasible";

    /// <summary>是否可继续推进（非 infeasible 即放行；uncertain 也交给导演终审）</summary>
    public bool IsFeasible => !string.Equals(Feasibility, "infeasible", StringComparison.OrdinalIgnoreCase);

    /// <summary>是否不可行（凭空且无任何依据，短路拒绝）</summary>
    public bool IsInfeasible => string.Equals(Feasibility, "infeasible", StringComparison.OrdinalIgnoreCase);

    /// <summary>是否存疑（引用了疑似曾获得但账本查无，需导演叙事终审）</summary>
    public bool IsUncertain => string.Equals(Feasibility, "uncertain", StringComparison.OrdinalIgnoreCase);

    /// <summary>不可行原因（仅当 infeasible 时由AI输出）</summary>
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
