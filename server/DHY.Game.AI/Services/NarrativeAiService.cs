using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Prompts;
using DHY.Game.AI.Utils;
using DHY.Game.Core.Entities;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 叙事AI服务
/// </summary>
public class NarrativeAiService : ITransient
{
    private readonly AiModelFactory _modelFactory;
    private readonly PromptTemplateService _promptService;
    private readonly SqlSugarRepository<GameAiCallLog> _aiLogRep;
    private readonly ILogger<NarrativeAiService> _logger;

    public NarrativeAiService(
        AiModelFactory modelFactory,
        PromptTemplateService promptService,
        SqlSugarRepository<GameAiCallLog> aiLogRep,
        ILogger<NarrativeAiService> logger)
    {
        _modelFactory = modelFactory;
        _promptService = promptService;
        _aiLogRep = aiLogRep;
        _logger = logger;
    }

    /// <summary>
    /// 生成完整叙事文本
    /// </summary>
    public async Task<string> GenerateNarrativeAsync(NarrativeInput input, long? sessionId = null)
    {
        var sw = Stopwatch.StartNew();
        var modelType = input.IsAdult ? "AdultNarrative" : "Narrative";
        var config = _modelFactory.GetModelConfig(modelType);
        var aiRole = input.IsAdult ? "AdultNarrative" : "Narrative";

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain(aiRole, $"开始叙事生成, 场景类型: {input.SceneType}, 叙事种子: {(input.DirectorBlueprint?.NarrativeSeed?.Length > 50 ? input.DirectorBlueprint.NarrativeSeed[..50] + "..." : input.DirectorBlueprint?.NarrativeSeed)}");

        try
        {
            var client = _modelFactory.CreateClient(config);

            // 章节档：按分镜分段顺序生成后拼接为整章
            if (IsChapterScale(input))
                return await GenerateChapterNarrativeAsync(input, config, aiRole, client, sw, sessionId);

            var messages = BuildNarrativeMessages(input);
            var result = await client.ChatCompletionAsync(messages, config, aiRole: aiRole);

            sw.Stop();
            await LogAiCallAsync(sessionId, config.ModelId, result, sw.ElapsedMilliseconds, input.IsAdult);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("叙事AI调用失败: {Error}", result.ErrorMessage);
                return $"[叙事生成失败: {result.ErrorMessage}]";
            }

            if (_modelFactory.IsDebugEnabled)
                AiDebugLogger.LogCallChain(aiRole, $"叙事完成, 字数: {result.Content.Length}");

            return result.Content;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "叙事AI服务异常");
            return "[叙事生成异常]";
        }
    }

    /// <summary>
    /// 流式生成叙事
    /// </summary>
    public async IAsyncEnumerable<string> StreamNarrativeAsync(
        NarrativeInput input,
        long? sessionId = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var modelType = input.IsAdult ? "AdultNarrative" : "Narrative";
        var config = _modelFactory.GetModelConfig(modelType);
        var aiRole = input.IsAdult ? "AdultNarrative" : "Narrative";
        var client = _modelFactory.CreateClient(config);

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain(aiRole, $"开始流式叙事, 场景类型: {input.SceneType}, 分档: {input.DirectorBlueprint?.BeatScale}");

        // 章节档：按分镜逐段顺序流式生成，段间插入空行分隔，chunk 依次 yield，Hub/前端无感
        if (IsChapterScale(input))
        {
            await foreach (var chunk in StreamChapterBeatsCoreAsync(input, client, config, aiRole, 0, "", ct))
                yield return chunk;

            sw.Stop();
            if (_modelFactory.IsDebugEnabled)
                AiDebugLogger.LogCallChain(aiRole, $"章节档流式叙事完成, 分镜{input.DirectorBlueprint!.Beats!.Count}段, 耗时{sw.ElapsedMilliseconds}ms");
            yield break;
        }

        var singleMessages = BuildNarrativeMessages(input);

        await foreach (var chunk in client.StreamChatCompletionAsync(singleMessages, config, ct, aiRole: aiRole))
        {
            yield return chunk;
        }

        sw.Stop();
        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain(aiRole, $"流式叙事完成, 耗时{sw.ElapsedMilliseconds}ms");
    }

    private List<ChatMessage> BuildNarrativeMessages(NarrativeInput input)
    {
        // 构造公共上下文
        var recentNarrative = string.IsNullOrEmpty(input.RecentNarrative) ? "（无历史叙事）" : input.RecentNarrative;
        var worldContext = string.IsNullOrEmpty(input.WorldContext)
            ? ""
            : $"[世界背景]\n{input.WorldContext}";
        var playerInventory = string.IsNullOrEmpty(input.PlayerInventory)
            ? ""
            : $"[玩家当前装备]\n{input.PlayerInventory}";
        // 角色名称：有名称时用名称，无名称时用“无名旅行者”避免AI自行编造
        var characterName = string.IsNullOrEmpty(input.CharacterName) ? "无名旅行者" : input.CharacterName;

        // 成人轮与正常轮走同一套“导演蓝图驱动”流程，仅切换提示词模板
        // （narrative_adult_system 与 narrative_system 占位符完全一致，均消费 director_blueprint）
        var templateName = input.IsAdult ? "narrative_adult_system" : "narrative_system";
        var normalTemplate = _promptService.LoadTemplate(templateName);
        var npcConstraints = BuildNpcLanguageConstraints(input.NpcLanguageCards);
        var blueprintText = BuildBlueprintText(input);

        var normalContent = _promptService.RenderTemplate(normalTemplate, new Dictionary<string, string>
        {
            { "world_context", worldContext },
            { "player_inventory", playerInventory },
            { "character_name", characterName },
            { "npc_language_constraints", npcConstraints },
            { "director_blueprint", blueprintText },
            { "prose_guidance", BuildProseGuidanceText(input) },
            { "scene_style_module", !string.IsNullOrEmpty(input.SceneStyleModule) ? input.SceneStyleModule : BuildSceneStyleModule(input.SceneType) },
            { "style_bible", !string.IsNullOrEmpty(input.StyleBible) ? input.StyleBible : "" },
            { "motif_tracker", !string.IsNullOrEmpty(input.MotifTracker) ? input.MotifTracker : "" },
            { "recent_narrative", recentNarrative },
            { "word_target", (input.WordTarget > 0 ? input.WordTarget : 200).ToString() }
        });

        return new List<ChatMessage>
        {
            new() { Role = "system", Content = normalContent },
            new() { Role = "user", Content = $"场景类型: {input.SceneType}，本轮目标字数约 {(input.WordTarget > 0 ? input.WordTarget : 200)} 字，请生成叙事文本。" }
        };
    }

    /// <summary>
    /// 是否为章节档（beat_scale=chapter 且导演输出了分镜表；成人轮同样适用）
    /// </summary>
    public static bool IsChapterScale(NarrativeInput input)
    {
        return input.DirectorBlueprint != null
            && string.Equals(input.DirectorBlueprint.BeatScale, "chapter", StringComparison.OrdinalIgnoreCase)
            && input.DirectorBlueprint.Beats is { Count: > 0 };
    }

    /// <summary>
    /// 章节档：按分镜表逐段顺序生成（非流式），拼接为整章
    /// </summary>
    private async Task<string> GenerateChapterNarrativeAsync(
        NarrativeInput input,
        DHY.Game.AI.Options.AiModelConfig config,
        string aiRole,
        IAiModelClient client,
        Stopwatch sw,
        long? sessionId)
    {
        var beats = input.DirectorBlueprint!.Beats!;
        var totalTarget = input.WordTarget > 0 ? input.WordTarget : 2000;
        var perBeat = Math.Max(300, totalTarget / beats.Count);
        var chapterSoFar = new StringBuilder();
        var totalInput = 0;
        var totalOutput = 0;

        for (var i = 0; i < beats.Count; i++)
        {
            var messages = BuildChapterBeatMessages(input, beats[i], chapterSoFar.ToString(), perBeat, i, beats.Count);
            var beatResult = await client.ChatCompletionAsync(messages, config, aiRole: aiRole);
            totalInput += beatResult.InputTokens;
            totalOutput += beatResult.OutputTokens;

            if (!beatResult.IsSuccess)
            {
                _logger.LogWarning("章节档分镜生成失败(第{Index}/{Total}段): {Error}", i + 1, beats.Count, beatResult.ErrorMessage);
                continue;
            }

            if (chapterSoFar.Length > 0)
                chapterSoFar.Append("\n\n");
            chapterSoFar.Append(beatResult.Content.Trim());
        }

        sw.Stop();
        var full = chapterSoFar.ToString();
        await LogAiCallAsync(sessionId, config.ModelId,
            new AiCompletionResult { IsSuccess = true, Content = full, InputTokens = totalInput, OutputTokens = totalOutput },
            sw.ElapsedMilliseconds, input.IsAdult);

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain(aiRole, $"章节档叙事完成, 分镜{beats.Count}段, 字数: {full.Length}");

        return full;
    }

    /// <summary>
    /// 章节档分镜流式生成核心：从 startIndex 起逐段流式输出，seedText 作为已写正文种子（用于续写上下文）。
    /// 首段之外的每段前插入空行分隔（i>0）；续写时 startIndex>0 则首个输出块即为分隔空行，与前缀自然衔接。
    /// </summary>
    private async IAsyncEnumerable<string> StreamChapterBeatsCoreAsync(
        NarrativeInput input,
        IAiModelClient client,
        DHY.Game.AI.Options.AiModelConfig config,
        string aiRole,
        int startIndex,
        string seedText,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var beats = input.DirectorBlueprint!.Beats!;
        var totalTarget = input.WordTarget > 0 ? input.WordTarget : 2000;
        var perBeat = Math.Max(300, totalTarget / beats.Count);
        var chapterSoFar = new StringBuilder(seedText ?? "");

        for (var i = Math.Max(0, startIndex); i < beats.Count; i++)
        {
            if (i > 0)
            {
                // 段落分隔（与 narrative_system “空一行隔开”的排版约定一致）
                chapterSoFar.Append("\n\n");
                yield return "\n\n";
            }

            var messages = BuildChapterBeatMessages(input, beats[i], chapterSoFar.ToString(), perBeat, i, beats.Count);
            await foreach (var chunk in client.StreamChatCompletionAsync(messages, config, ct, aiRole: aiRole))
            {
                chapterSoFar.Append(chunk);
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// 章节档预取：非流式生成前 prefetchBeats 个分镜，拼接为前缀文本。
    /// 供预计算使用；返回（前缀文本, 下一段起始索引）。
    /// </summary>
    public async Task<(string prefixText, int nextBeatIndex)> GenerateChapterPrefixAsync(
        NarrativeInput input, int prefetchBeats, long? sessionId = null)
    {
        var beats = input.DirectorBlueprint?.Beats;
        if (beats == null || beats.Count == 0)
            return ("", -1);

        var sw = Stopwatch.StartNew();
        var aiRole = input.IsAdult ? "AdultNarrative" : "Narrative";
        var config = _modelFactory.GetModelConfig(aiRole);
        var client = _modelFactory.CreateClient(config);

        var count = Math.Clamp(prefetchBeats, 1, beats.Count);
        var totalTarget = input.WordTarget > 0 ? input.WordTarget : 2000;
        var perBeat = Math.Max(300, totalTarget / beats.Count);
        var chapterSoFar = new StringBuilder();
        var totalInput = 0;
        var totalOutput = 0;

        for (var i = 0; i < count; i++)
        {
            var messages = BuildChapterBeatMessages(input, beats[i], chapterSoFar.ToString(), perBeat, i, beats.Count);
            var beatResult = await client.ChatCompletionAsync(messages, config, aiRole: aiRole);
            totalInput += beatResult.InputTokens;
            totalOutput += beatResult.OutputTokens;

            if (!beatResult.IsSuccess)
            {
                _logger.LogWarning("章节档预取分镜失败(第{Index}/{Total}段): {Error}", i + 1, beats.Count, beatResult.ErrorMessage);
                continue;
            }

            if (chapterSoFar.Length > 0)
                chapterSoFar.Append("\n\n");
            chapterSoFar.Append(beatResult.Content.Trim());
        }

        sw.Stop();
        var prefix = chapterSoFar.ToString();
        await LogAiCallAsync(sessionId, config.ModelId,
            new AiCompletionResult { IsSuccess = true, Content = prefix, InputTokens = totalInput, OutputTokens = totalOutput },
            sw.ElapsedMilliseconds, input.IsAdult);

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain(aiRole, $"章节档预取完成, 预取{count}/{beats.Count}段, 前缀字数: {prefix.Length}");

        return (prefix, count);
    }

    /// <summary>
    /// 章节档续写：从 startIndex 起实时流式生成剩余分镜，seedText 为已预取的前缀正文（作为上下文）。
    /// 供 Hub 命中缓存时回放前缀后无缝接上实时续写使用。
    /// </summary>
    public async IAsyncEnumerable<string> StreamChapterContinuationAsync(
        NarrativeInput input,
        int startIndex,
        string seedText,
        long? sessionId = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var beats = input.DirectorBlueprint?.Beats;
        if (beats == null || startIndex >= beats.Count)
            yield break;

        var aiRole = input.IsAdult ? "AdultNarrative" : "Narrative";
        var config = _modelFactory.GetModelConfig(aiRole);
        var client = _modelFactory.CreateClient(config);

        await foreach (var chunk in StreamChapterBeatsCoreAsync(input, client, config, aiRole, startIndex, seedText, ct))
            yield return chunk;
    }

    /// <summary>
    /// 构造章节档单个分段的消息：突出本段细纲 + 附整章总细纲参考 + 已写正文作为连贯上下文
    /// </summary>
    private List<ChatMessage> BuildChapterBeatMessages(
        NarrativeInput input, ChapterBeatInfo beat, string chapterSoFar,
        int beatWordTarget, int index, int total)
    {
        var normalTemplate = _promptService.LoadTemplate(input.IsAdult ? "narrative_adult_system" : "narrative_system");
        var npcConstraints = BuildNpcLanguageConstraints(input.NpcLanguageCards);
        var characterName = string.IsNullOrEmpty(input.CharacterName) ? "无名旅行者" : input.CharacterName;
        var worldContext = string.IsNullOrEmpty(input.WorldContext) ? "" : $"[世界背景]\n{input.WorldContext}";
        var playerInventory = string.IsNullOrEmpty(input.PlayerInventory) ? "" : $"[玩家当前装备]\n{input.PlayerInventory}";

        // 本段细纲：突出本段分段细纲 + 附整章总细纲参考
        var beatBlueprint = new StringBuilder();
        beatBlueprint.AppendLine($"【本段细纲】{beat.Seed}");
        if (!string.IsNullOrEmpty(beat.BeatType))
            beatBlueprint.AppendLine($"【本段类型】{beat.BeatType}");
        if (!string.IsNullOrEmpty(beat.Focus))
            beatBlueprint.AppendLine($"【本段要交代清楚的事】{beat.Focus}");
        beatBlueprint.AppendLine();
        beatBlueprint.AppendLine("【整章总细纲参考（只用于了解前后文，不要写其他段的内容）】");
        beatBlueprint.Append(BuildBlueprintText(input));

        // 连贯上下文：原历史 + 本章已写正文（取末尾，避免上下文膨胀）
        var priorContext = input.RecentNarrative ?? "";
        if (!string.IsNullOrEmpty(chapterSoFar))
            priorContext = string.IsNullOrEmpty(priorContext) ? chapterSoFar : priorContext + "\n\n" + chapterSoFar;
        priorContext = TrimTail(priorContext, 1200);
        if (string.IsNullOrEmpty(priorContext))
            priorContext = "（无历史叙事）";

        var content = _promptService.RenderTemplate(normalTemplate, new Dictionary<string, string>
        {
            { "world_context", worldContext },
            { "player_inventory", playerInventory },
            { "character_name", characterName },
            { "npc_language_constraints", npcConstraints },
            { "director_blueprint", beatBlueprint.ToString() },
            { "prose_guidance", BuildProseGuidanceText(input) },
            { "scene_style_module", !string.IsNullOrEmpty(input.SceneStyleModule) ? input.SceneStyleModule : BuildSceneStyleModule(input.SceneType) },
            { "style_bible", !string.IsNullOrEmpty(input.StyleBible) ? input.StyleBible : "" },
            { "motif_tracker", !string.IsNullOrEmpty(input.MotifTracker) ? input.MotifTracker : "" },
            { "recent_narrative", priorContext },
            { "word_target", beatWordTarget.ToString() }
        });

        var positionNote = index == 0
            ? "这是本章的开篇段落，负责铺陈与带入，不要收尾。"
            : index == total - 1
                ? "这是本章的最后一段，负责推向高潮或收束，并在结尾留出玩家行动空间。"
                : "这是本章的中间段落，承接上文继续推进，不要收尾、不要重复上文已出现的台词与动作。";

        var beatTypeLabel = string.IsNullOrEmpty(beat.BeatType) ? "" : $"（{beat.BeatType}）";
        var userMsg =
            $"你正在续写同一章小说的第 {index + 1}/{total} 段{beatTypeLabel}。{positionNote}\n" +
            $"与【最近叙事】自然衔接。本段必须把【本段细纲】里的事件全部写完（不能只写开头就停），也不要提前写后面分段的内容，更不要自己发明细纲里没有的人物和事件。\n" +
            $"本段目标约 {beatWordTarget} 字，写完事件字数不够就加对话和玩家的直接感受，不要加描写。请直接输出本段叙事文本：";

        return new List<ChatMessage>
        {
            new() { Role = "system", Content = content },
            new() { Role = "user", Content = userMsg }
        };
    }

    /// <summary>取字符串末尾至多 maxLen 个字符（用于控制连贯上下文长度）</summary>
    private static string TrimTail(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLen)
            return text ?? "";
        return text[^maxLen..];
    }

    private static string BuildNpcLanguageConstraints(List<NpcLanguageCardDto> cards)
    {
        if (cards == null || cards.Count == 0)
            return "[当前场景无NPC]";

        var sb = new StringBuilder("[当前场景NPC语言约束]\n");
        foreach (var card in cards)
        {
            sb.AppendLine($"- {card.NpcName}: 风格「{card.LanguageStyle}」，口头禅「{card.Catchphrase}」，态度:{card.CurrentAttitude}");
        }
        return sb.ToString();
    }

    private static string BuildBlueprintText(NarrativeInput input)
    {
        var sb = new StringBuilder();
        var blueprint = input.DirectorBlueprint;

        sb.AppendLine($"剧情细纲: {blueprint.NarrativeSeed}");

        if (blueprint.Pacing != null)
            sb.AppendLine($"紧张度: {blueprint.Pacing.TensionLevel}/10 ({blueprint.Pacing.Note})");

        if (blueprint.NpcActions != null && blueprint.NpcActions.Count > 0)
        {
            sb.AppendLine("NPC行动:");
            foreach (var npc in blueprint.NpcActions)
            {
                sb.AppendLine($"  - {npc.NpcId}: {npc.Action}");
                if (npc.DialogueDirection != null)
                {
                    var d = npc.DialogueDirection;
                    if (!string.IsNullOrEmpty(d.Surface))
                        sb.AppendLine($"    台词: {d.Surface}");
                    if (!string.IsNullOrEmpty(d.Subtext))
                        sb.AppendLine($"    潜台词: {d.Subtext}");
                    if (!string.IsNullOrEmpty(d.Conceal))
                        sb.AppendLine($"    隐瞒: {d.Conceal}");
                    if (!string.IsNullOrEmpty(d.BodyLanguage))
                        sb.AppendLine($"    身体语言: {d.BodyLanguage}");
                }
            }
        }

        if (blueprint.NarrativeHooks != null && blueprint.NarrativeHooks.Count > 0)
        {
            sb.AppendLine("叙事钩子:");
            foreach (var hook in blueprint.NarrativeHooks)
                sb.AppendLine($"  - {hook}");
        }

        return sb.ToString();
    }

    private static string BuildProseGuidanceText(NarrativeInput input)
    {
        if (string.IsNullOrEmpty(input.DirectorBlueprint?.ProseGuidance))
            return "";
        return $"[本轮写法提示]\n{input.DirectorBlueprint.ProseGuidance}";
    }

    /// <summary>
    /// 根据场景类型生成对应的文风模块指导（方案K：场景类型驱动文风切换）
    /// </summary>
    private static string BuildSceneStyleModule(string sceneType)
    {
        return sceneType switch
        {
            "action" or "critical" =>
                "[战斗/紧张场景文风]\n" +
                "节奏：动作快写、结果直写。一个回合的攻防一两句话写完，谁打了谁、打中没打中、现在谁占上风，读者一眼看明白。\n" +
                "感受：允许直接写玩家的紧张和判断（“你知道躲不开了”“你心里一紧”），一句带过，接着写动作。\n" +
                "禁忌：不拆分解动作、不写精确尺寸角度秒数、不做慢镜头、不堆比喻。一个疼的细节（“虎口震得发麻”）就够了。\n",
            "dialogue" =>
                "[对话交互场景文风]\n" +
                "节奏：以台词为主体，台词之间用一两句动作或表情衔接，不要在两句台词之间塞一大段描写。\n" +
                "感受：玩家对NPC话里意思的判断可以直接写（“你听出他在敷衍”），一句话点到即止。\n" +
                "禁忌：NPC的每个小动作只写一次、只写一句；不分析潜台词，让读者从台词和动作的矛盾里自己看出来。\n",
            "opening" or "exploration" =>
                "[探索/入场场景文风]\n" +
                "节奏：先用两三句把地方交代清楚（哪里、什么样、有什么），再挑一个最有意思的细节写细。\n" +
                "感受：写玩家看到这地方的第一反应（“这地方比你想的更破”），让读者跟着玩家的眼睛走。\n" +
                "禁忌：不用长从句铺陈环境、不堆叠感官描写、不把环境拟人化成“活的存在”。\n",
            "horror" =>
                "[恐怖场景文风]\n" +
                "节奏：正常叙述中突然出现一件不对的事，说清楚哪里不对，然后停住让玩家反应。\n" +
                "感受：直接写玩家的害怕（“你后背一凉”“你不敢回头”），这是恐怖感最直接的来源。\n" +
                "禁忌：不用“某种”“什么东西”这类模糊指代装神秘；不写感官失真、不写残缺句；恐怖来自事实本身不对劲，不来自文字含糊。\n",
            _ =>
                "[日常场景文风]\n" +
                "节奏：口语化，像讲故事一样说事，把该发生的事说完。\n" +
                "感受：玩家的想法和情绪可以自然带出（“你有点犯困”“你懒得理他”）。\n" +
                "禁忌：不为了氛围写景，一两句生活细节点一下就往下走。\n"
        };
    }

    private async Task LogAiCallAsync(long? sessionId, string modelName, AiCompletionResult result, long durationMs, bool isAdult = false)
    {
        try
        {
            var log = new GameAiCallLog
            {
                SessionId = sessionId,
                AiType = isAdult ? "adult_narrative" : "narrative",
                ModelName = modelName,
                InputTokens = result.InputTokens,
                OutputTokens = result.OutputTokens,
                TotalTokens = result.InputTokens + result.OutputTokens,
                DurationMs = (int)durationMs,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage,
                Cost = (result.InputTokens * 0.004m + result.OutputTokens * 0.012m) / 1000
            };
            await _aiLogRep.AsInsertable(log).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("AI日志记录失败: {Error}", ex.Message);
        }
    }
}
