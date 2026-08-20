namespace DHY.Game.AIEval.Reporting;

/// <summary>
/// 基线对比：baseline.json 记录每个用例上次基线的通过状态。
/// 回归 = 基线通过且本次失败；改善 = 基线失败且本次通过。
/// 仅对本次实际运行的用例做对比（未运行的用例不产生回归判定）。
/// </summary>
public static class BaselineComparer
{
    private static string BaselinePath => Path.Combine(ReportGenerator.ReportsDirectory, "baseline.json");

    /// <summary>加载基线（不存在时返回空字典）</summary>
    public static Dictionary<string, bool> Load()
    {
        if (!File.Exists(BaselinePath)) return new();
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, bool>>(File.ReadAllText(BaselinePath)) ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[基线] baseline.json 解析失败，忽略: {ex.Message}");
            return new();
        }
    }

    /// <summary>对比本次运行结果与基线，填充回归/改善清单</summary>
    public static void Compare(EvalRunResult run, Dictionary<string, bool> baseline)
    {
        foreach (var suite in run.Suites)
        {
            foreach (var c in suite.Cases)
            {
                if (!baseline.TryGetValue(c.CaseId, out var baselinePassed))
                    continue; // 新用例无基线，不参与回归判定

                if (baselinePassed && !c.Passed)
                    run.Regressions.Add(c.CaseId);
                else if (!baselinePassed && c.Passed)
                    run.Improvements.Add(c.CaseId);
            }
        }
    }

    /// <summary>
    /// 更新基线：合并本次运行结果（未运行的用例保留旧基线值）
    /// </summary>
    public static void Update(EvalRunResult run)
    {
        var baseline = Load();
        foreach (var suite in run.Suites)
            foreach (var c in suite.Cases)
                baseline[c.CaseId] = c.Passed;

        Directory.CreateDirectory(ReportGenerator.ReportsDirectory);
        File.WriteAllText(BaselinePath, JsonConvert.SerializeObject(baseline, Formatting.Indented));
        Console.WriteLine($"[基线] 已更新 baseline.json（{baseline.Count} 个用例）");
    }
}
