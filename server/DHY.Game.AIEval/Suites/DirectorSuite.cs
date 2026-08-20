using DHY.Game.AIEval.Infrastructure;

namespace DHY.Game.AIEval.Suites;

/// <summary>
/// 导演AI评测集：结构化输出完整性（种子/文风指导/对话四层/建议选项/分镜表）、
/// hint纪律、成败一致性（judge层）。
/// </summary>
public class DirectorSuite : EvalSuiteBase<DirectorCase>
{
    public override string Name => "director";

    private static readonly string[] ValidBeatScales = { "micro", "normal", "chapter" };

    protected override async Task<EvalCaseResult> RunCaseAsync(EvalHost host, DirectorCase c, EvalRunOptions options)
    {
        var result = new EvalCaseResult { CaseId = c.Id, Description = c.Desc };

        var input = new DirectorInput
        {
            PlayerAction = c.Input.PlayerAction,
            ActionIntent = c.Input.ActionIntent,
            WorldState = c.Input.WorldState,
            DungeonContext = c.Input.DungeonContext,
            NpcProfiles = c.Input.NpcProfiles,
            MainQuestProgress = c.Input.MainQuestProgress,
            PlayerInventory = c.Input.PlayerInventory,
            IsRoutine = c.Input.IsRoutine,
            CharacterName = c.Input.CharacterName,
            JudgmentOutcome = c.Input.JudgmentOutcome,
            NeedsStateChange = c.Input.NeedsStateChange,
            SideQuestList = c.Input.SideQuestList,
            HiddenContentList = c.Input.HiddenContentList
        };

        var output = await host.Director.DirectAsync(input, EvalHost.EvalSessionId);
        if (output == null)
        {
            result.Errored = true;
            result.Error = "导演AI返回null（调用失败或JSON解析失败）";
            return result;
        }

        var e = c.Expect;

        // 1. 叙事种子非空且长度合理（150-400字，过短=干骨架，过长=越界当正文写）
        if (string.IsNullOrWhiteSpace(output.NarrativeSeed))
            result.Checks.Add(CheckResult.Fail("叙事种子非空", "非空", "空"));
        else if (output.NarrativeSeed.Length < 100 || output.NarrativeSeed.Length > 500)
            result.Checks.Add(CheckResult.Fail("叙事种子长度", "[100,500]", output.NarrativeSeed.Length.ToString()));
        else
            result.Checks.Add(CheckResult.Ok("叙事种子长度", $"{output.NarrativeSeed.Length}字"));

        // 2. 文风指导非空
        if (!string.IsNullOrWhiteSpace(output.ProseGuidance))
            result.Checks.Add(CheckResult.Ok("文风指导", "非空"));
        else
            result.Checks.Add(CheckResult.Fail("文风指导", "非空", "空"));

        // 3. 节拍分档合法；指定期望时精确匹配
        var scale = output.BeatScale?.ToLowerInvariant() ?? "";
        if (!ValidBeatScales.Contains(scale))
            result.Checks.Add(CheckResult.Fail("节拍分档合法", string.Join("/", ValidBeatScales), scale));
        else
            result.Checks.Add(CheckResult.Ok("节拍分档合法", scale));

        if (!string.IsNullOrEmpty(e.ExpectBeatScale))
        {
            if (scale == e.ExpectBeatScale.ToLowerInvariant())
                result.Checks.Add(CheckResult.Ok("期望节拍分档", scale));
            else
                result.Checks.Add(CheckResult.Fail("期望节拍分档", e.ExpectBeatScale, scale));
        }

        // 4. 章节档必须输出4-8个分镜，且每段种子非空
        if (scale == "chapter")
        {
            if (output.Beats is { Count: >= 4 and <= 8 } && output.Beats.All(b => !string.IsNullOrWhiteSpace(b.Seed)))
                result.Checks.Add(CheckResult.Ok("章节分镜表", $"{output.Beats.Count}段"));
            else
                result.Checks.Add(CheckResult.Fail("章节分镜表", "4-8段且种子非空",
                    output.Beats == null ? "无分镜" : $"{output.Beats.Count}段"));
        }

        // 5. 建议行动选项纪律：恰好2个，文本≤15字
        if (output.SuggestedActions is { Count: 2 } sa
            && sa.All(a => !string.IsNullOrWhiteSpace(a.ActionText) && a.ActionText.Length <= 15))
            result.Checks.Add(CheckResult.Ok("建议行动选项", $"[{string.Join(" | ", sa.Select(a => a.ActionText))}]"));
        else
            result.Checks.Add(CheckResult.Fail("建议行动选项", "恰好2个且各≤15字",
                output.SuggestedActions == null ? "未输出" :
                $"{output.SuggestedActions.Count}个: {string.Join(" | ", output.SuggestedActions.Select(a => $"{a.ActionText}({a.ActionText?.Length}字)"))}"));

        // 6. NPC对话指导四层结构（期望对话的场景检查）
        if (e.ExpectDialogue == true)
        {
            var dialogues = output.NpcActions?.Where(n => n.DialogueDirection != null).ToList();
            if (dialogues is { Count: > 0 } && dialogues.All(d => !string.IsNullOrWhiteSpace(d.DialogueDirection!.Surface)))
                result.Checks.Add(CheckResult.Ok("对话指导四层", $"{dialogues.Count}个NPC"));
            else
                result.Checks.Add(CheckResult.Fail("对话指导四层", "至少1个NPC含dialogue_direction且surface非空",
                    output.NpcActions == null ? "无npc_actions" : $"{dialogues?.Count ?? 0}个"));
        }

        // 7. 状态变更摘要必出（需要状态变更时）
        if (c.Input.NeedsStateChange)
        {
            if (!string.IsNullOrWhiteSpace(output.WorldStateChanges?.Summary))
                result.Checks.Add(CheckResult.Ok("状态变更摘要", "summary非空"));
            else
                result.Checks.Add(CheckResult.Fail("状态变更摘要", "summary非空", "缺失"));
        }

        // 8. hint纪律：期望包含/禁止的名称
        var hintNames = output.ItemHints?.Select(h => h.Name).ToList() ?? new();
        if (e.ExpectHintNames is { Count: > 0 })
        {
            var missing = e.ExpectHintNames
                .Where(n => !hintNames.Any(h => h.Contains(n, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (missing.Count == 0)
                result.Checks.Add(CheckResult.Ok("关键资产hint", $"[{string.Join(",", hintNames)}]"));
            else
                result.Checks.Add(CheckResult.Fail("关键资产hint", $"含[{string.Join(",", e.ExpectHintNames)}]", $"实际[{string.Join(",", hintNames)}]"));
        }
        if (e.ForbidHintNames is { Count: > 0 })
        {
            var violated = e.ForbidHintNames
                .Where(n => hintNames.Any(h => h.Contains(n, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (violated.Count == 0)
                result.Checks.Add(CheckResult.Ok("普通物品不hint", "无违规"));
            else
                result.Checks.Add(CheckResult.Fail("普通物品不hint", $"不含[{string.Join(",", e.ForbidHintNames)}]", $"违规[{string.Join(",", violated)}]"));
        }

        // 9. judge层：叙事种子与骰子成败一致性
        if (e.JudgmentSuccess.HasValue && options.EnableJudge)
        {
            var judge = new JudgeService(host);
            var verdict = await judge.JudgeSeedOutcomeAsync(output.NarrativeSeed, e.JudgmentSuccess.Value);
            if (verdict == null)
                result.Checks.Add(CheckResult.Fail("成败一致性(judge)", "judge可解析", "judge调用/解析失败", "judge"));
            else if (verdict.Consistent)
                result.Checks.Add(new CheckResult { Name = "成败一致性(judge)", Passed = true, Layer = "judge", Actual = verdict.Reason ?? "一致" });
            else
                result.Checks.Add(CheckResult.Fail("成败一致性(judge)",
                    e.JudgmentSuccess.Value ? "成功走向" : "失败走向", verdict.Reason ?? "不一致", "judge"));
        }

        return result;
    }
}
