using DHY.Game.AIEval.Infrastructure;
using DHY.Game.AIEval.Reporting;
using DHY.Game.AIEval.Suites;

namespace DHY.Game.AIEval;

/// <summary>
/// AI质量回归评测工具入口
/// 修改 Prompt 模板 / 更换模型配置后运行，量化回答"质量变好还是变坏"。
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var options = EvalRunOptions.Parse(args);
        if (options == null)
            return 2;

        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.WriteLine($"AI质量回归评测  suite={options.Suite}  judge={(options.EnableJudge ? "on" : "off")}" +
            (options.CaseId != null ? $"  case={options.CaseId}" : ""));

        EvalHost host;
        try
        {
            host = EvalHost.Build(options.ConfigPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[致命] 评测宿主初始化失败: {ex.Message}");
            return 2;
        }

        using (host)
        {
            var run = new EvalRunResult
            {
                RunAt = DateTime.Now,
                JudgeEnabled = options.EnableJudge
            };

            IEvalSuite[] suites =
            {
                new ClassifierSuite(),
                new DirectorSuite(),
                new QuartermasterSuite(),
                new NarrativeSuite()
            };

            try
            {
                foreach (var suite in suites)
                {
                    if (!options.ShouldRunSuite(suite.Name)) continue;
                    var suiteResult = await suite.RunAsync(host, options);
                    run.Suites.Add(suiteResult);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[致命] 评测执行中断: {ex}");
                return 2;
            }

            if (run.TotalCases == 0)
            {
                Console.WriteLine("[警告] 没有执行任何用例");
                return 2;
            }

            // 基线对比 → 报告 → （可选）更新基线
            var baseline = BaselineComparer.Load();
            BaselineComparer.Compare(run, baseline);
            var reportPath = ReportGenerator.Write(run);
            Console.WriteLine($"[报告完成] {reportPath}");

            if (options.UpdateBaseline)
                BaselineComparer.Update(run);
            else if (run.Regressions.Count > 0 || run.Improvements.Count > 0)
                Console.WriteLine("[提示] 如需以本次结果为基线，追加 --update-baseline 运行");

            var failed = run.TotalCases - run.TotalPassed;
            return (failed > 0 || run.HasRegression) ? 1 : 0;
        }
    }
}
