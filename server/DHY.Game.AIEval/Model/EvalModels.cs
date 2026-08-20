namespace DHY.Game.AIEval.Model;

/// <summary>
/// 单项检查结果（一个用例包含多项检查）
/// </summary>
public class CheckResult
{
    /// <summary>检查项名称（如 可行性三态 / 种子长度 / 禁用陈词）</summary>
    public string Name { get; set; } = "";

    /// <summary>是否通过</summary>
    public bool Passed { get; set; }

    /// <summary>期望值描述</summary>
    public string Expected { get; set; } = "";

    /// <summary>实际值描述</summary>
    public string Actual { get; set; } = "";

    /// <summary>检查层级：rule=确定性规则 judge=LLM评审</summary>
    public string Layer { get; set; } = "rule";

    public static CheckResult Ok(string name, string detail = "") =>
        new() { Name = name, Passed = true, Actual = detail };

    public static CheckResult Fail(string name, string expected, string actual, string layer = "rule") =>
        new() { Name = name, Passed = false, Expected = expected, Actual = actual, Layer = layer };
}

/// <summary>
/// 单用例评测结果
/// </summary>
public class EvalCaseResult
{
    /// <summary>所属评测集（classifier/director/quartermaster/narrative）</summary>
    public string Suite { get; set; } = "";

    /// <summary>用例ID（全局唯一）</summary>
    public string CaseId { get; set; } = "";

    /// <summary>用例描述</summary>
    public string Description { get; set; } = "";

    /// <summary>是否执行出错（AI调用异常/解析失败等，区别于检查不通过）</summary>
    public bool Errored { get; set; }

    /// <summary>出错信息</summary>
    public string? Error { get; set; }

    /// <summary>全部检查项</summary>
    public List<CheckResult> Checks { get; set; } = new();

    /// <summary>耗时毫秒</summary>
    public long DurationMs { get; set; }

    /// <summary>用例是否通过（无错误且全部检查通过）</summary>
    public bool Passed => !Errored && Checks.Count > 0 && Checks.All(c => c.Passed);

    /// <summary>失败/出错的检查项摘要（报告用）</summary>
    public string FailureSummary => Errored
        ? $"ERROR: {Error}"
        : string.Join("; ", Checks.Where(c => !c.Passed)
            .Select(c => $"{c.Name}(期望:{c.Expected} 实际:{Truncate(c.Actual, 80)})"));

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : (s.Length <= max ? s : s[..max] + "...");
}

/// <summary>
/// 单评测集运行结果
/// </summary>
public class SuiteResult
{
    public string Suite { get; set; } = "";
    public List<EvalCaseResult> Cases { get; set; } = new();

    public int Total => Cases.Count;
    public int PassedCount => Cases.Count(c => c.Passed);
    public int FailedCount => Total - PassedCount;
    public double PassRate => Total == 0 ? 0 : (double)PassedCount / Total * 100;
}

/// <summary>
/// 整次评测运行结果
/// </summary>
public class EvalRunResult
{
    public DateTime RunAt { get; set; }
    public string ConfigSource { get; set; } = "";
    public bool JudgeEnabled { get; set; }
    public List<SuiteResult> Suites { get; set; } = new();
    /// <summary>相对基线的回归用例（基线通过、本次失败）</summary>
    public List<string> Regressions { get; set; } = new();
    /// <summary>相对基线的改善用例（基线失败、本次通过）</summary>
    public List<string> Improvements { get; set; } = new();

    public int TotalCases => Suites.Sum(s => s.Total);
    public int TotalPassed => Suites.Sum(s => s.PassedCount);
    public bool HasRegression => Regressions.Count > 0;
}
