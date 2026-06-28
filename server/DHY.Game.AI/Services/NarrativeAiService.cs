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
            AiDebugLogger.LogCallChain(aiRole, $"开始叙事生成, 场景类型: {input.SceneType}, 叙事方向: {input.DirectorBlueprint?.NarrativeDirection}");

        try
        {
            var messages = BuildNarrativeMessages(input);
            var client = _modelFactory.CreateClient(config);
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
        var messages = BuildNarrativeMessages(input);
        var client = _modelFactory.CreateClient(config);

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain(aiRole, $"开始流式叙事, 场景类型: {input.SceneType}");

        await foreach (var chunk in client.StreamChatCompletionAsync(messages, config, ct, aiRole: aiRole))
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

        // 成人内容：使用独立提示词模板，不依赖导演蓝图
        if (input.IsAdult)
        {
            var template = _promptService.LoadTemplate("narrative_adult_system");
            var systemContent = _promptService.RenderTemplate(template, new Dictionary<string, string>
            {
                { "world_context", worldContext },
                { "player_inventory", playerInventory },
                { "character_name", characterName },
                { "player_action", input.PlayerAction },
                { "recent_narrative", recentNarrative }
            });

            return new List<ChatMessage>
            {
                new() { Role = "system", Content = systemContent },
                new() { Role = "user", Content = "请生成叙事文本。" }
            };
        }

        // 正常叙事：使用导演蓝图指导
        var normalTemplate = _promptService.LoadTemplate("narrative_system");
        var npcConstraints = BuildNpcLanguageConstraints(input.NpcLanguageCards);
        var blueprintText = BuildBlueprintText(input);

        var normalContent = _promptService.RenderTemplate(normalTemplate, new Dictionary<string, string>
        {
            { "world_context", worldContext },
            { "player_inventory", playerInventory },
            { "character_name", characterName },
            { "npc_language_constraints", npcConstraints },
            { "director_blueprint", blueprintText },
            { "recent_narrative", recentNarrative }
        });

        return new List<ChatMessage>
        {
            new() { Role = "system", Content = normalContent },
            new() { Role = "user", Content = $"场景类型: {input.SceneType}，请生成叙事文本。" }
        };
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

        sb.AppendLine($"叙事方向: {blueprint.NarrativeDirection}");

        if (blueprint.Pacing != null)
            sb.AppendLine($"紧张度: {blueprint.Pacing.TensionLevel}/10 ({blueprint.Pacing.Note})");

        if (!string.IsNullOrEmpty(blueprint.SensoryHint))
            sb.AppendLine($"感官重点: {blueprint.SensoryHint}");

        if (blueprint.NpcActions != null && blueprint.NpcActions.Count > 0)
        {
            sb.AppendLine("NPC行动:");
            foreach (var npc in blueprint.NpcActions)
            {
                sb.AppendLine($"  - {npc.NpcId}: {npc.Action}");
                if (!string.IsNullOrEmpty(npc.DialogueGist))
                    sb.AppendLine($"    台词大意: {npc.DialogueGist}");
            }
        }

        // 判定结果已由导演AI融入叙事方向，叙事AI仅从导演的NarrativeDirection感知成败

        return sb.ToString();
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
