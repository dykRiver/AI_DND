using System.Text;

namespace DHY.Game.AIEval.Reporting;

/// <summary>
/// 评测报告生成：控制台汇总 + Markdown（人读）+ JSON（机器读/基线源）
/// </summary>
public static class ReportGenerator
{
    public static string ReportsDirectory => Path.Combine(AppContext.BaseDirectory, "reports");

    /// <summary>
    /// 输出全部报告，返回 Markdown 报告路径
    /// </summary>
    public static string Write(EvalRunResult run)
    {
        Directory.CreateDirectory(ReportsDirectory);
        var stamp = run.RunAt.ToString("yyyyMMdd_HHmmss");
        var mdPath = Path.Combine(ReportsDirectory, $"eval_{stamp}.md");
        var jsonPath = Path.Combine(ReportsDirectory, $"eval_{stamp}.json");

        File.WriteAllText(mdPath, BuildMarkdown(run), Encoding.UTF8);
        File.WriteAllText(jsonPath, JsonConvert.SerializeObject(run, Formatting.Indented), Encoding.UTF8);

        PrintConsoleSummary(run);
        Console.WriteLine($"\n[报告] Markdown: {mdPath}");
        Console.WriteLine($"[报告] JSON:     {jsonPath}");
        return mdPath;
    }

    private static void PrintConsoleSummary(EvalRunResult run)
    {
        Console.WriteLine("\n" + new string('═', 62));
        Console.WriteLine($"评测汇总  {run.RunAt:yyyy-MM-dd HH:mm:ss}  judge={(run.JudgeEnabled ? "开" : "关")}");
        Console.WriteLine(new string('─', 62));
        Console.WriteLine($"{"评测集",-14}{"通过",-8}{"失败",-8}{"通过率",-10}");
        foreach (var s in run.Suites)
            Console.WriteLine($"{s.Suite,-14}{s.PassedCount,-8}{s.FailedCount,-8}{s.PassRate:F1}%");
        Console.WriteLine(new string('─', 62));
        Console.WriteLine($"总计: {run.TotalPassed}/{run.TotalCases} 通过");

        if (run.Regressions.Count > 0)
        {
            Console.WriteLine($"\n[回归告警] {run.Regressions.Count} 个基线通过用例本次失败:");
            run.Regressions.ForEach(id => Console.WriteLine($"  ✗ {id}"));
        }
        if (run.Improvements.Count > 0)
        {
            Console.WriteLine($"\n[改善] {run.Improvements.Count} 个基线失败用例本次通过:");
            run.Improvements.ForEach(id => Console.WriteLine($"  ✓ {id}"));
        }
        Console.WriteLine(new string('═', 62));
    }

    private static string BuildMarkdown(EvalRunResult run)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# AI质量回归评测报告");
        sb.AppendLine();
        sb.AppendLine($"> 运行时间：{run.RunAt:yyyy-MM-dd HH:mm:ss} | judge：{(run.JudgeEnabled ? "开" : "关")} | 配置：`{run.ConfigSource}`");
        sb.AppendLine();
        sb.AppendLine($"**总计：{run.TotalPassed}/{run.TotalCases} 通过**");
        if (run.Regressions.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"## 回归告警（{run.Regressions.Count}）");
            run.Regressions.ForEach(id => sb.AppendLine($"- {id}"));
        }
        if (run.Improvements.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine($"## 改善（{run.Improvements.Count}）");
            run.Improvements.ForEach(id => sb.AppendLine($"- {id}"));
        }

        foreach (var s in run.Suites)
        {
            sb.AppendLine();
            sb.AppendLine($"## {s.Suite}（{s.PassedCount}/{s.Total}）");
            sb.AppendLine();
            sb.AppendLine("| 结果 | 用例 | 耗时 | 说明 |");
            sb.AppendLine("|------|------|------|------|");
            foreach (var c in s.Cases)
            {
                var mark = c.Passed ? "PASS" : "FAIL";
                var detail = c.Passed ? c.Description : $"{c.Description} → {EscapeCell(c.FailureSummary)}";
                sb.AppendLine($"| {mark} | {c.CaseId} | {c.DurationMs}ms | {EscapeCell(detail)} |");
            }

            // 失败用例的逐项检查明细
            var failed = s.Cases.Where(c => !c.Passed).ToList();
            if (failed.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("<details><summary>失败检查明细</summary>");
                sb.AppendLine();
                foreach (var c in failed)
                {
                    sb.AppendLine($"### {c.CaseId}");
                    if (c.Errored)
                    {
                        sb.AppendLine($"- ERROR: {c.Error}");
                    }
                    else
                    {
                        foreach (var chk in c.Checks.Where(chk => !chk.Passed))
                            sb.AppendLine($"- [{chk.Layer}] {chk.Name}: 期望 `{chk.Expected}` 实际 `{chk.Actual}`");
                    }
                    sb.AppendLine();
                }
                sb.AppendLine("</details>");
            }
        }

        return sb.ToString();
    }

    private static string EscapeCell(string s) =>
        s.Replace("|", "\\|").Replace("\r", "").Replace("\n", " ");
}
