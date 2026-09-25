using DHY.Game.AIEval.Infrastructure;

namespace DHY.Game.AIEval.Suites;

/// <summary>
/// GM(前台推演)AI评测集：结构化输出完整性（剧情细纲/写法提示/对话四层/分段细纲）、
/// 成败一致性（judge层）。状态记账字段（world_state_changes/item_hints/suggested_actions）
/// 已拆给书记官，不再在此评测。
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
            CharacterName = c.Input.CharacterName,
            JudgmentOutcome = c.Input.JudgmentOutcome,
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

        // 1. 剧情细纲非空且长度合理（150-400字，过短=干骨架，过长=越界当正文写）
        if (string.IsNullOrWhiteSpace(output.NarrativeSeed))
            result.Checks.Add(CheckResult.Fail("剧情细纲非空", "非空", "空"));
        else if (output.NarrativeSeed.Length < 100 || output.NarrativeSeed.Length > 500)
            result.Checks.Add(CheckResult.Fail("剧情细纲长度", "[100,500]", output.NarrativeSeed.Length.ToString()));
        else
            result.Checks.Add(CheckResult.Ok("剧情细纲长度", $"{output.NarrativeSeed.Length}字"));

        // 2. 写法提示非空
        if (!string.IsNullOrWhiteSpace(output.ProseGuidance))
            result.Checks.Add(CheckResult.Ok("写法提示", "非空"));
        else
            result.Checks.Add(CheckResult.Fail("写法提示", "非空", "空"));

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

        // 4. 章节档必须输出4-8个分段细纲，且每段细纲非空
        if (scale == "chapter")
        {
            if (output.Beats is { Count: >= 4 and <= 8 } && output.Beats.All(b => !string.IsNullOrWhiteSpace(b.Seed)))
                result.Checks.Add(CheckResult.Ok("章节分段细纲", $"{output.Beats.Count}段"));
            else
                result.Checks.Add(CheckResult.Fail("章节分段细纲", "4-8段且细纲非空",
                    output.Beats == null ? "无分段" : $"{output.Beats.Count}段"));
        }

        // 5. 建议行动选项已拆给书记官（suggested_actions），GM不再检查

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

        // 7/8. 状态变更摘要与hint纪律已拆给书记官（world_state_changes/item_hints），前台导演不再检查

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
