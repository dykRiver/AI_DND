using DHY.Game.AIEval.Infrastructure;

namespace DHY.Game.AIEval.Suites;

/// <summary>
/// 分类AI评测集：三态可行性门卫准确率（误杀/漏放）、常规行动分类、成人判定、检定参数合理性。
/// 纯规则层判定。
/// </summary>
public class ClassifierSuite : EvalSuiteBase<ClassifierCase>
{
    public override string Name => "classifier";

    protected override async Task<EvalCaseResult> RunCaseAsync(EvalHost host, ClassifierCase c, EvalRunOptions options)
    {
        var result = new EvalCaseResult { CaseId = c.Id, Description = c.Desc };

        var actual = await host.Classifier.ClassifyAsync(c.PlayerInput, c.Scenario, c.Inventory, c.NpcProfiles);
        if (actual == null)
        {
            result.Errored = true;
            result.Error = "分类AI返回null";
            return result;
        }

        var e = c.Expect;

        // 1. 可行性三态（核心：门卫误杀/漏放）
        if (!string.IsNullOrEmpty(e.Feasibility))
        {
            if (string.Equals(actual.Feasibility, e.Feasibility, StringComparison.OrdinalIgnoreCase))
                result.Checks.Add(CheckResult.Ok("可行性三态", actual.Feasibility));
            else
                result.Checks.Add(CheckResult.Fail("可行性三态", e.Feasibility!, actual.Feasibility));
        }

        // 2. 常规行动分类
        if (e.IsRoutine.HasValue)
        {
            if (actual.IsRoutine == e.IsRoutine.Value)
                result.Checks.Add(CheckResult.Ok("常规行动分类", actual.IsRoutine.ToString()));
            else
                result.Checks.Add(CheckResult.Fail("常规行动分类", e.IsRoutine.ToString() ?? "", actual.IsRoutine.ToString()));
        }

        // 3. 成人内容判定
        if (e.IsAdult.HasValue)
        {
            if (actual.IsAdult == e.IsAdult.Value)
                result.Checks.Add(CheckResult.Ok("成人内容判定", actual.IsAdult.ToString()));
            else
                result.Checks.Add(CheckResult.Fail("成人内容判定", e.IsAdult.ToString() ?? "", actual.IsAdult.ToString()));
        }

        // 4. 是否需要检定
        var hasJudgment = actual.Judgment is { Needed: true };
        if (e.JudgmentNeeded.HasValue)
        {
            if (hasJudgment == e.JudgmentNeeded.Value)
                result.Checks.Add(CheckResult.Ok("检定触发", hasJudgment.ToString()));
            else
                result.Checks.Add(CheckResult.Fail("检定触发", e.JudgmentNeeded.Value.ToString(), hasJudgment.ToString()));
        }

        // 5. DC区间（DC标尺校准：5-25合理区间内）
        if (e.DcRange is { Length: 2 } && actual.Judgment != null)
        {
            var dc = actual.Judgment.Dc;
            if (dc >= e.DcRange[0] && dc <= e.DcRange[1])
                result.Checks.Add(CheckResult.Ok("DC区间", $"DC={dc}"));
            else
                result.Checks.Add(CheckResult.Fail("DC区间", $"[{e.DcRange[0]},{e.DcRange[1]}]", $"DC={dc}"));
        }

        // 6. 技能匹配
        if (!string.IsNullOrEmpty(e.SkillHint) && actual.Judgment != null)
        {
            var skill = actual.Judgment.Skill ?? "";
            if (skill.Contains(e.SkillHint, StringComparison.OrdinalIgnoreCase))
                result.Checks.Add(CheckResult.Ok("技能匹配", skill));
            else
                result.Checks.Add(CheckResult.Fail("技能匹配", $"含\"{e.SkillHint}\"", skill));
        }

        // 兜底：无任何检查项视为用例配置错误
        if (result.Checks.Count == 0)
        {
            result.Errored = true;
            result.Error = "用例未配置任何期望字段";
        }

        return result;
    }
}
