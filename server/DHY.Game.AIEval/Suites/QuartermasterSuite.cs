using DHY.Game.AIEval.Infrastructure;

namespace DHY.Game.AIEval.Suites;

/// <summary>
/// 物资官（道具AI）评测集：蓝图逐条落实（不丢不多）、数值补全、情报/道具分流。
/// 纯规则层判定。评测用伪会话ID，角色表为空 => 落库自动跳过，只验证记账增量。
/// </summary>
public class QuartermasterSuite : EvalSuiteBase<QuartermasterCase>
{
    public override string Name => "quartermaster";

    protected override async Task<EvalCaseResult> RunCaseAsync(EvalHost host, QuartermasterCase c, EvalRunOptions options)
    {
        var result = new EvalCaseResult { CaseId = c.Id, Description = c.Desc };

        var delta = await host.Quartermaster.RecordFromBlueprintAsync(
            EvalHost.EvalSessionId, c.PlayerAction, c.ItemHintsText, c.CurrentLedger, 1, c.Blueprint);

        if (delta == null)
        {
            result.Errored = true;
            result.Error = "记账返回null（AI调用失败且保底未生效，或解析失败）";
            return result;
        }

        var e = c.Expect;
        var acquiredNames = delta.AcquiredItems?.Select(i => i.ItemName).ToList() ?? new();
        var consumedNames = delta.ConsumedItems?.Select(i => i.ItemName).ToList() ?? new();
        var lostNames = delta.LostItems?.Select(i => i.ItemName).ToList() ?? new();
        var infoNames = delta.AcquiredInfo?.Select(i => i.Name).ToList() ?? new();
        var invalidNames = delta.InvalidatedInfo?.Select(i => i.Name).ToList() ?? new();

        // 1. 各分组条目齐全（包含匹配）
        CheckNamesContain(result, "获得道具齐全", e.AcquiredItemNames, acquiredNames);
        CheckNamesContain(result, "消耗道具齐全", e.ConsumedItemNames, consumedNames);
        CheckNamesContain(result, "遗失道具齐全", e.LostItemNames, lostNames);
        CheckNamesContain(result, "获得情报齐全", e.AcquiredInfoNames, infoNames);
        CheckNamesContain(result, "失效情报齐全", e.InvalidatedInfoNames, invalidNames);

        // 2. 道具纪律：不丢不多（蓝图外不得新增）
        if (e.ForbidExtra)
        {
            var blueprintNames = c.Blueprint.Select(b => b.Name).ToList();
            var extras = acquiredNames.Concat(consumedNames).Concat(lostNames)
                .Concat(infoNames).Concat(invalidNames)
                .Where(n => !string.IsNullOrEmpty(n))
                .Where(n => !blueprintNames.Any(b => n.Contains(b, StringComparison.OrdinalIgnoreCase)
                    || b.Contains(n, StringComparison.OrdinalIgnoreCase)))
                .Distinct().ToList();
            if (extras.Count == 0)
                result.Checks.Add(CheckResult.Ok("蓝图外无新增", "不丢不多"));
            else
                result.Checks.Add(CheckResult.Fail("蓝图外无新增", "仅蓝图条目", string.Join(",", extras)));
        }

        // 3. 数值补全（获得物理道具类型非空、weight不为负；关键道具 weight=0 是模板允许的设计）
        if (e.RequireItemNumeric && delta.AcquiredItems is { Count: > 0 })
        {
            var incomplete = delta.AcquiredItems
                .Where(i => i.Weight < 0 || string.IsNullOrEmpty(i.ItemType))
                .Select(i => $"{i.ItemName}(weight={i.Weight},type={i.ItemType})")
                .ToList();
            if (incomplete.Count == 0)
                result.Checks.Add(CheckResult.Ok("数值字段完整", $"{delta.AcquiredItems.Count}项均已补全"));
            else
                result.Checks.Add(CheckResult.Fail("数值字段完整", "weight≥0且类型非空", string.Join(",", incomplete)));
        }

        // 4. 情报不得混入物理道具分组（情报类蓝图条目不应出现在道具分组）
        if (e.InfoNotAsItem && e.AcquiredInfoNames is { Count: > 0 })
        {
            var leaked = acquiredNames
                .Where(n => e.AcquiredInfoNames.Any(info => n.Contains(info, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (leaked.Count == 0)
                result.Checks.Add(CheckResult.Ok("情报道具分流", "情报未混入背包"));
            else
                result.Checks.Add(CheckResult.Fail("情报道具分流", "情报仅进账本", $"混入背包:{string.Join(",", leaked)}"));
        }

        return result;
    }

    private static void CheckNamesContain(EvalCaseResult result, string checkName, List<string>? expected, List<string> actual)
    {
        if (expected is not { Count: > 0 }) return;

        var missing = expected
            .Where(name => !actual.Any(a => a.Contains(name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (missing.Count == 0)
            result.Checks.Add(CheckResult.Ok(checkName, $"[{string.Join(",", actual)}]"));
        else
            result.Checks.Add(CheckResult.Fail(checkName, $"含[{string.Join(",", expected)}]", $"实际[{string.Join(",", actual)}]，缺失[{string.Join(",", missing)}]"));
    }
}
