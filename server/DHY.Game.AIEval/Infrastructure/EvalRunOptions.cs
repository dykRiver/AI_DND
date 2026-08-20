namespace DHY.Game.AIEval.Infrastructure;

/// <summary>
/// 命令行运行选项
/// </summary>
public class EvalRunOptions
{
    /// <summary>运行的评测集（all/classifier/director/quartermaster/narrative）</summary>
    public string Suite { get; set; } = "all";

    /// <summary>是否启用 LLM-as-judge 主观评审层</summary>
    public bool EnableJudge { get; set; } = true;

    /// <summary>仅运行指定用例ID（调试用）</summary>
    public string? CaseId { get; set; }

    /// <summary>评测集内用例并发数（防限流）</summary>
    public int Concurrency { get; set; } = 2;

    /// <summary>AI配置文件路径（为空时自动查找）</summary>
    public string? ConfigPath { get; set; }

    /// <summary>运行结束后将本次结果写入基线</summary>
    public bool UpdateBaseline { get; set; }

    /// <summary>评测集运行顺序</summary>
    public static readonly string[] SuiteOrder = { "classifier", "director", "quartermaster", "narrative" };

    public bool ShouldRunSuite(string suiteName) =>
        string.Equals(Suite, "all", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Suite, suiteName, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 解析命令行参数。返回 null 表示参数非法（已打印用法）。
    /// </summary>
    public static EvalRunOptions? Parse(string[] args)
    {
        var opt = new EvalRunOptions();
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--suite":
                    if (!Next(args, ref i, out var suite)) return null;
                    opt.Suite = suite;
                    break;
                case "--judge":
                    if (!Next(args, ref i, out var judge)) return null;
                    opt.EnableJudge = judge.Equals("on", StringComparison.OrdinalIgnoreCase);
                    break;
                case "--case":
                    if (!Next(args, ref i, out var caseId)) return null;
                    opt.CaseId = caseId;
                    break;
                case "--concurrency":
                    if (!Next(args, ref i, out var c) || !int.TryParse(c, out var n) || n < 1)
                    {
                        Console.WriteLine("[参数] --concurrency 需要正整数");
                        return null;
                    }
                    opt.Concurrency = Math.Min(n, 8);
                    break;
                case "--config":
                    if (!Next(args, ref i, out var cfg)) return null;
                    opt.ConfigPath = cfg;
                    break;
                case "--update-baseline":
                    opt.UpdateBaseline = true;
                    break;
                case "-h":
                case "--help":
                    PrintUsage();
                    return null;
                default:
                    Console.WriteLine($"[参数] 未知参数: {args[i]}");
                    PrintUsage();
                    return null;
            }
        }

        if (!SuiteOrder.Contains(opt.Suite.ToLowerInvariant()) && opt.Suite != "all")
        {
            Console.WriteLine($"[参数] --suite 取值: all/{string.Join("/", SuiteOrder)}");
            return null;
        }
        return opt;
    }

    private static bool Next(string[] args, ref int i, out string value)
    {
        if (i + 1 < args.Length) { value = args[++i]; return true; }
        value = "";
        Console.WriteLine("[参数] 选项缺少取值");
        return false;
    }

    public static void PrintUsage()
    {
        Console.WriteLine("""
            AI质量回归评测工具
            用法: dotnet run --project DHY.Game.AIEval -- [选项]
              --suite <all|classifier|director|quartermaster|narrative>  评测集（默认all）
              --judge <on|off>       LLM主观评审层（默认on）
              --case <id>            只跑单个用例（调试）
              --concurrency <n>      用例并发数（默认2，上限8）
              --config <path>        GameAiOptions.json 路径（默认自动查找）
              --update-baseline      运行结束后将本次结果写入基线
            退出码: 0=全部通过且无回归; 1=存在失败或回归
            """);
    }
}
