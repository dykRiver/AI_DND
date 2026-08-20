using DHY.Game.AIEval.Infrastructure;

namespace DHY.Game.AIEval.Suites;

/// <summary>
/// 叙事AI评测集：字数区间、禁用陈词、玩家名保真、信息泄露（规则层）+ 文学品质评审（judge层）。
/// </summary>
public class NarrativeSuite : EvalSuiteBase<NarrativeCase>
{
    public override string Name => "narrative";

    protected override async Task<EvalCaseResult> RunCaseAsync(EvalHost host, NarrativeCase c, EvalRunOptions options)
    {
        var result = new EvalCaseResult { CaseId = c.Id, Description = c.Desc };

        var input = new NarrativeInput
        {
            DirectorBlueprint = c.Input.Blueprint,
            NpcLanguageCards = c.Input.NpcLanguageCards,
            RecentNarrative = c.Input.RecentNarrative,
            SceneType = c.Input.SceneType,
            WordTarget = c.Input.WordTarget,
            WorldContext = c.Input.WorldContext,
            PlayerInventory = c.Input.PlayerInventory,
            CharacterName = c.Input.CharacterName,
            StyleBible = c.Input.StyleBible,
            MotifTracker = c.Input.MotifTracker
        };

        var text = await host.Narrative.GenerateNarrativeAsync(input, EvalHost.EvalSessionId);
        if (string.IsNullOrEmpty(text) || text.StartsWith("[叙事生成失败") || text.StartsWith("[叙事生成异常"))
        {
            result.Errored = true;
            result.Error = text?.Length > 200 ? text[..200] : text;
            return result;
        }

        var e = c.Expect;

        // 1. 字数区间（±容差由用例给定）
        if (e.WordRange is { Length: 2 })
        {
            if (text.Length >= e.WordRange[0] && text.Length <= e.WordRange[1])
                result.Checks.Add(CheckResult.Ok("字数区间", $"{text.Length}字"));
            else
                result.Checks.Add(CheckResult.Fail("字数区间", $"[{e.WordRange[0]},{e.WordRange[1]}]", $"{text.Length}字"));
        }

        // 2. 禁用陈词（文风圣经 forbidden_cliches 纪律）
        if (e.ForbiddenWords is { Count: > 0 })
        {
            var hit = e.ForbiddenWords.Where(w => text.Contains(w, StringComparison.Ordinal)).ToList();
            if (hit.Count == 0)
                result.Checks.Add(CheckResult.Ok("禁用陈词", "无命中"));
            else
                result.Checks.Add(CheckResult.Fail("禁用陈词", $"不含[{string.Join(",", e.ForbiddenWords)}]", $"命中[{string.Join(",", hit)}]"));
        }

        // 3. 必须出现（玩家角色名保真等）
        if (e.MustMention is { Count: > 0 })
        {
            var missing = e.MustMention.Where(m => !text.Contains(m, StringComparison.Ordinal)).ToList();
            if (missing.Count == 0)
                result.Checks.Add(CheckResult.Ok("必含文本", $"[{string.Join(",", e.MustMention)}]"));
            else
                result.Checks.Add(CheckResult.Fail("必含文本", $"含[{string.Join(",", e.MustMention)}]", $"缺失[{string.Join(",", missing)}]"));
        }

        // 4. 信息泄露检查（NPC隐瞒内容等不得出现在玩家可见叙事中）
        if (e.ForbiddenFacts is { Count: > 0 })
        {
            var leaked = e.ForbiddenFacts.Where(f => text.Contains(f, StringComparison.OrdinalIgnoreCase)).ToList();
            if (leaked.Count == 0)
                result.Checks.Add(CheckResult.Ok("信息不泄露", "无命中"));
            else
                result.Checks.Add(CheckResult.Fail("信息不泄露", $"不含[{string.Join(",", e.ForbiddenFacts)}]", $"泄露[{string.Join(",", leaked)}]"));
        }

        // 5. judge层：文风遵循 / 种子一致性 / 感官节奏 + 玩家名保真
        if (e.Judge && options.EnableJudge)
        {
            var judge = new JudgeService(host);
            var verdict = await judge.JudgeNarrativeAsync(
                c.Input.StyleBible,
                c.Input.Blueprint.NarrativeSeed,
                c.Input.Blueprint.ProseGuidance,
                text,
                c.Input.CharacterName);

            if (verdict == null)
                result.Checks.Add(CheckResult.Fail("文学品质(judge)", "judge可解析", "judge调用/解析失败", "judge"));
            else if (verdict.Passed)
                result.Checks.Add(new CheckResult { Name = "文学品质(judge)", Passed = true, Layer = "judge", Actual = verdict.Detail });
            else
                result.Checks.Add(CheckResult.Fail("文学品质(judge)", "均分≥4且单项≥3", verdict.Detail, "judge"));

            // 玩家名保真（judge 审查 NPC 称呼，历史缺陷模式：自创昵称如“阿坤”；第二人称正文不出现玩家名属正常）
            if (verdict != null)
            {
                if (verdict.FabricatedPlayerName)
                    result.Checks.Add(CheckResult.Fail("玩家名保真(judge)", $"NPC称呼仅限『{c.Input.CharacterName}』", "NPC编造了玩家称呼", "judge"));
                else
                    result.Checks.Add(new CheckResult { Name = "玩家名保真(judge)", Passed = true, Layer = "judge", Actual = $"未发现编造称呼（合法名：{c.Input.CharacterName}）" });
            }
        }

        return result;
    }
}
