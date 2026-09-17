using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Prompts;
using DHY.Game.AI.Utils;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 书记官AI服务 - 状态记账执行器。
/// 与叙事流式并行运行：依前台导演输出的既定事实，产出 world_state_changes / item_hints /
/// npc_attitude_changes / suggested_actions 等全部结构化状态字段。无思考、低温度、纯执行。
/// </summary>
public class ScribeAiService : ITransient
{
    private readonly AiModelFactory _modelFactory;
    private readonly PromptTemplateService _promptService;
    private readonly SqlSugarRepository<GameAiCallLog> _aiLogRep;
    private readonly ILogger<ScribeAiService> _logger;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public ScribeAiService(
        AiModelFactory modelFactory,
        PromptTemplateService promptService,
        SqlSugarRepository<GameAiCallLog> aiLogRep,
        ILogger<ScribeAiService> logger)
    {
        _modelFactory = modelFactory;
        _promptService = promptService;
        _aiLogRep = aiLogRep;
        _logger = logger;
    }

    /// <summary>
    /// 书记官记账推演（失败自动重试1次，仍失败返回null：本轮状态不变、叙事不中断）
    /// </summary>
    public async Task<ScribeOutput?> ScribeAsync(ScribeInput input, long? sessionId = null)
    {
        for (var attempt = 0; attempt <= 1; attempt++)
        {
            var output = await ScribeOnceAsync(input, sessionId);
            if (output != null)
                return output;
            if (attempt == 0)
                _logger.LogWarning("书记官AI首次调用失败，重试1次: SessionId={SessionId}", sessionId);
        }
        _logger.LogError("书记官AI重试后仍失败，本轮状态不变: SessionId={SessionId}", sessionId);
        return null;
    }

    private async Task<ScribeOutput?> ScribeOnceAsync(ScribeInput input, long? sessionId)
    {
        var sw = Stopwatch.StartNew();
        var config = _modelFactory.GetModelConfig("Scribe");

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain("Scribe", $"开始书记官记账, 玩家行动: {input.PlayerAction}");

        try
        {
            var systemPrompt = _promptService.LoadTemplate(input.SuggestionsOnly ? "scribe_suggestions_system" : "scribe_system");

            // 构造上下文消息列表（按注意力权重排列，既定事实与玩家行动放最后）
            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt }
            };

            if (!string.IsNullOrEmpty(input.CharacterName))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[玩家角色名称]\n{input.CharacterName}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解玩家角色名称。" });
            }

            if (!string.IsNullOrEmpty(input.NpcProfiles))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[NPC档案]\n{input.NpcProfiles}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解NPC信息。" });
            }

            if (!string.IsNullOrEmpty(input.WorldState))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[当前世界状态(含历史)]\n{input.WorldState}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解当前状态。" });
            }

            if (!string.IsNullOrEmpty(input.MainQuestProgress))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[主线进度]\n{input.MainQuestProgress}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解主线进度。" });
            }

            if (!string.IsNullOrEmpty(input.SideQuestList))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[支线任务清单]\n{input.SideQuestList}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解支线任务清单。" });
            }

            if (!string.IsNullOrEmpty(input.HiddenContentList))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[隐藏内容清单]\n{input.HiddenContentList}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解隐藏内容清单。" });
            }

            if (!string.IsNullOrEmpty(input.PlayerInventory))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[玩家当前装备与道具]\n{input.PlayerInventory}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解玩家装备状态。" });
            }

            if (!string.IsNullOrEmpty(input.JudgmentOutcome))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[判定结果]\n{input.JudgmentOutcome}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解判定结果。" });
            }

            // 前台导演输出的既定事实（书记官只记录，不得改写剧情）
            messages.Add(new ChatMessage { Role = "user", Content = $"[本轮既定事实(前台导演输出)]\n{input.DirectorFacts}" });
            messages.Add(new ChatMessage { Role = "assistant", Content = "已了解本轮既定事实，我将据此如实记账。" });

            // 选项节奏档位（由代码层依导演输出判定：抉择点/章节档/高紧张为关键时刻）
            var pacingTier = input.IsKeyMoment ? "细粒度档（关键时刻）" : "粗粒度档（推进优先）";
            messages.Add(new ChatMessage { Role = "user", Content = $"[选项节奏档]\n{pacingTier}" });
            messages.Add(new ChatMessage { Role = "assistant", Content = "已了解选项节奏档，我将按对应粒度生成行动建议。" });

            var actionContent = !string.IsNullOrEmpty(input.ActionIntent)
                ? $"玩家行动意图: {input.ActionIntent}\n玩家原始表达: {input.PlayerAction}"
                : $"玩家行动: {input.PlayerAction}";
            messages.Add(new ChatMessage
            {
                Role = "user",
                Content = $"{actionContent}\n\n请输出记账JSON:"
            });

            var client = _modelFactory.CreateClient(config);
            var result = await client.ChatCompletionAsync(messages, config, aiRole: "Scribe");

            sw.Stop();
            await LogAiCallAsync(sessionId, config.ModelId, result, sw.ElapsedMilliseconds);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("书记官AI调用失败: {Error}", result.ErrorMessage);
                return null;
            }

            var output = ParseScribeOutput(result.Content);

            if (_modelFactory.IsDebugEnabled && output != null)
            {
                AiDebugLogger.LogCallChain("Scribe", $"状态摘要: {output.WorldStateChanges?.Summary}");
                AiDebugLogger.LogCallChain("Scribe", $"物资hint数: {output.ItemHints?.Count ?? 0}, 态度变化数: {output.NpcAttitudeChanges?.Count ?? 0}, 建议选项数: {output.SuggestedActions?.Count ?? 0}");
            }

            return output;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "书记官AI服务异常");
            await LogAiCallAsync(sessionId, config.ModelId,
                new AiCompletionResult { IsSuccess = false, ErrorMessage = ex.Message },
                sw.ElapsedMilliseconds);
            return null;
        }
    }

    private ScribeOutput? ParseScribeOutput(string content)
    {
        try
        {
            var cleaned = CleanJsonContent(content);
            return JsonConvert.DeserializeObject<ScribeOutput>(cleaned, _jsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("书记官输出解析失败: {Error}, 原始内容: {Content}", ex.Message, content);
            return null;
        }
    }

    private async Task LogAiCallAsync(long? sessionId, string modelName, AiCompletionResult result, long durationMs)
    {
        try
        {
            var log = new GameAiCallLog
            {
                SessionId = sessionId,
                AiType = "scribe",
                ModelName = modelName,
                InputTokens = result.InputTokens,
                OutputTokens = result.OutputTokens,
                TotalTokens = result.InputTokens + result.OutputTokens,
                DurationMs = (int)durationMs,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.ErrorMessage,
                Cost = EstimateCost(result.InputTokens, result.OutputTokens)
            };
            await _aiLogRep.AsInsertable(log).ExecuteCommandAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("AI日志记录失败: {Error}", ex.Message);
        }
    }

    private static decimal EstimateCost(int inputTokens, int outputTokens)
    {
        // 按通义千问定价估算 (仅供参考)
        return (inputTokens * 0.004m + outputTokens * 0.012m) / 1000;
    }

    private static string CleanJsonContent(string content)
    {
        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline > 0)
                content = content[(firstNewline + 1)..];
            if (content.EndsWith("```"))
                content = content[..^3];
        }
        return content.Trim();
    }
}
