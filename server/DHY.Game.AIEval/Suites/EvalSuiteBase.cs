using System.Diagnostics;
using DHY.Game.AIEval.Infrastructure;

namespace DHY.Game.AIEval.Suites;

/// <summary>
/// 评测集接口
/// </summary>
public interface IEvalSuite
{
    string Name { get; }
    Task<SuiteResult> RunAsync(EvalHost host, EvalRunOptions options);
}

/// <summary>
/// 评测集基类：加载案例、并发控制、异常兜底
/// </summary>
public abstract class EvalSuiteBase<TCase> : IEvalSuite where TCase : EvalCaseBase
{
    public abstract string Name { get; }

    /// <summary>执行单用例，返回检查结果</summary>
    protected abstract Task<EvalCaseResult> RunCaseAsync(EvalHost host, TCase c, EvalRunOptions options);

    public async Task<SuiteResult> RunAsync(EvalHost host, EvalRunOptions options)
    {
        var suiteResult = new SuiteResult { Suite = Name };

        List<TCase> cases = CaseLoader.Load<TCase>(Name);
        if (!string.IsNullOrEmpty(options.CaseId))
            cases = cases.Where(c => c.Id == options.CaseId).ToList();

        if (cases.Count == 0)
        {
            Console.WriteLine($"  [{Name}] 无匹配用例，跳过");
            return suiteResult;
        }

        Console.WriteLine($"  [{Name}] {cases.Count} 个用例，并发 {options.Concurrency}...");
        using var semaphore = new SemaphoreSlim(options.Concurrency);

        async Task<EvalCaseResult> RunWithGateAsync(TCase c)
        {
            await semaphore.WaitAsync();
            try
            {
                var sw = Stopwatch.StartNew();
                var result = await SafeRunAsync(host, c, options);
                sw.Stop();
                result.DurationMs = sw.ElapsedMilliseconds;
                result.Suite = Name;
                var mark = result.Passed ? "PASS" : "FAIL";
                Console.WriteLine($"  [{Name}] {mark} {c.Id} ({result.DurationMs}ms)");
                if (!result.Passed)
                    Console.WriteLine($"         ↳ {result.FailureSummary}");
                return result;
            }
            finally
            {
                _ = semaphore.Release();
            }
        }

        var results = await Task.WhenAll(cases.Select(RunWithGateAsync));
        suiteResult.Cases = results.OrderBy(r => r.CaseId).ToList();
        Console.WriteLine($"  [{Name}] 完成: {suiteResult.PassedCount}/{suiteResult.Total} 通过 ({suiteResult.PassRate:F1}%)");
        return suiteResult;
    }

    private async Task<EvalCaseResult> SafeRunAsync(EvalHost host, TCase c, EvalRunOptions options)
    {
        try
        {
            var result = await RunCaseAsync(host, c, options);
            result.CaseId = c.Id;
            result.Description = c.Desc;
            return result;
        }
        catch (Exception ex)
        {
            return new EvalCaseResult
            {
                CaseId = c.Id,
                Description = c.Desc,
                Errored = true,
                Error = ex.Message
            };
        }
    }
}
