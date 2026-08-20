using DHY.Game.AIEval.Infrastructure;

namespace DHY.Game.AIEval.Suites;

/// <summary>
/// LLM-as-judge：用强模型（Director 同款配置）对主观维度打分/审查。
/// judge 本身有波动，故只用于补充规则层无法覆盖的维度，且评审失败不静默吞掉（记为 judge 层失败项）。
/// </summary>
public class JudgeService
{
    private readonly EvalHost _host;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public JudgeService(EvalHost host)
    {
        _host = host;
    }

    /// <summary>
    /// 叙事文学品质评审：文风遵循 / 种子一致性 / 感官节奏，各 1-5 分；另审查 NPC 是否编造玩家称呼（名保真）。
    /// </summary>
    public async Task<NarrativeJudgeVerdict?> JudgeNarrativeAsync(
        string styleBible, string narrativeSeed, string proseGuidance, string narrativeText, string characterName)
    {
        var system = $$"""
            你是TRPG叙事质量评审。请严格按以下三个维度对【生成叙事】打分（1-5整数），并输出JSON：
            1. style_compliance：是否遵循文风圣经的语调/句式/感官调色板，是否避开了禁用陈词
            2. seed_consistency：是否忠实于导演叙事种子的事件与氛围走向（可扩写细节，不得篡改事实或添加种子外的重要事件）
            3. sensory_rhythm：感官细节是否具体可感，句式节奏是否有变化与张力
            评分锚点：5=出版级 4=优秀，细节小瑕 3=合格，有明显套路化 2=偏弱，违背指导 1=严重跑题
            另需输出布尔字段 fabricated_player_name：玩家唯一合法名字是「{{characterName}}」（第二人称叙事正文不出现玩家名是正常的）。
            若NPC称呼玩家时使用了任何编造的名字/昵称（如“阿坤”“小子”式的自创专名），输出 true；无称呼行为或正确称呼输出 false。
            只输出JSON，不要其他文字：
            {"style_compliance": 0, "seed_consistency": 0, "sensory_rhythm": 0, "fabricated_player_name": false, "issues": ["问题简述，没有则空数组"]}
            """;

        var user =
            $"【文风圣经】\n{styleBible}\n\n" +
            $"【导演叙事种子】\n{narrativeSeed}\n\n" +
            $"【文风指导】\n{proseGuidance}\n\n" +
            $"【生成叙事】\n{narrativeText}";

        var json = await CallJudgeAsync(system, user);
        if (json == null) return null;

        try
        {
            return JsonConvert.DeserializeObject<NarrativeJudgeVerdict>(CleanJson(json), _jsonSettings);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 导演种子与骰子成败一致性审查：成功判定 → 种子不得呈现失败结局，反之亦然。
    /// </summary>
    public async Task<SeedOutcomeVerdict?> JudgeSeedOutcomeAsync(string narrativeSeed, bool expectSuccess)
    {
        var system = """
            你是TRPG导演输出审查员。给定一次行动的骰子判定结果语义与导演生成的叙事种子，
            判断叙事种子的剧情走向是否与判定结果一致：
            - 判定成功时：种子中的关键行动应呈现达成/得手/奏效的走向（可以附带代价或新状况，但主结果必须是成功）
            - 判定失败时：种子中的关键行动应呈现落空/受挫/事与愿违的走向（可以有意外收获，但主结果必须是失败）
            只输出JSON：{"consistent": true或false, "reason": "一句话理由"}
            """;

        var user =
            $"【骰子判定结果】{(expectSuccess ? "成功" : "失败")}\n\n" +
            $"【导演叙事种子】\n{narrativeSeed}";

        var json = await CallJudgeAsync(system, user);
        if (json == null) return null;

        try
        {
            return JsonConvert.DeserializeObject<SeedOutcomeVerdict>(CleanJson(json), _jsonSettings);
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> CallJudgeAsync(string system, string user)
    {
        var config = _host.ModelFactory.GetModelConfig("Director");
        var client = _host.ModelFactory.CreateClient(config);
        var messages = new List<ChatMessage>
        {
            new() { Role = "system", Content = system },
            new() { Role = "user", Content = user }
        };
        var result = await client.ChatCompletionAsync(messages, config, aiRole: "Judge");
        return result.IsSuccess ? result.Content : null;
    }

    private static string CleanJson(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline > 0) content = content[(firstNewline + 1)..];
            if (content.EndsWith("```")) content = content[..^3];
        }
        return content.Trim();
    }
}

/// <summary>叙事评审结论（1-5分，通过线：三项均分≥4 且单项不低于3；另含玩家名保真审查）</summary>
public class NarrativeJudgeVerdict
{
    public int StyleCompliance { get; set; }
    public int SeedConsistency { get; set; }
    public int SensoryRhythm { get; set; }
    /// <summary>NPC是否编造玩家称呼（历史缺陷模式：自创昵称如“阿坤”）</summary>
    public bool FabricatedPlayerName { get; set; }
    public List<string>? Issues { get; set; }

    public double Average => (StyleCompliance + SeedConsistency + SensoryRhythm) / 3.0;
    public bool Passed => Average >= 4.0 &&
        StyleCompliance >= 3 && SeedConsistency >= 3 && SensoryRhythm >= 3;

    public string Detail => $"文风={StyleCompliance} 种子一致={SeedConsistency} 感官节奏={SensoryRhythm} 均分={Average:F1}" +
        (Issues is { Count: > 0 } ? $" 问题:{string.Join(";", Issues)}" : "");
}

/// <summary>导演种子与成败一致性结论</summary>
public class SeedOutcomeVerdict
{
    public bool Consistent { get; set; }
    public string? Reason { get; set; }
}
