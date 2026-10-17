using System.Diagnostics;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Prompts;
using DHY.Game.AI.Utils;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 玩家辅导员AI服务 - 玩家意图的代理人。
/// 把玩家模糊的长期目标翻译为「当前世界状态下最该走的下一步」(next_focus)，并滚动评估目标进度、管理目标全生命周期。
/// 与书记官同源读取「世界状态快照 + 导演既定事实」，串在书记官之前（真实轮Full跑、预计算DryRun跳过复用）。
/// 纪律：不写剧情、不改世界、不产具体行动选项（那是书记官的职责）。
/// </summary>
public class PlayerCoachService : ITransient
{
    private readonly AiModelFactory _modelFactory;
    private readonly PromptTemplateService _promptService;
    private readonly SqlSugarRepository<GameAiCallLog> _aiLogRep;
    private readonly ILogger<PlayerCoachService> _logger;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public PlayerCoachService(
        AiModelFactory modelFactory,
        PromptTemplateService promptService,
        SqlSugarRepository<GameAiCallLog> aiLogRep,
        ILogger<PlayerCoachService> logger)
    {
        _modelFactory = modelFactory;
        _promptService = promptService;
        _aiLogRep = aiLogRep;
        _logger = logger;
    }

    /// <summary>
    /// 分析玩家目标（失败自动重试1次，仍失败返回null：书记官回退原始目标、记账链不中断）。
    /// ct 供调用方施加超时降级（超时触发 OperationCanceledException → 返回null → 书记官回退原始目标）。
    /// </summary>
    public async Task<CoachOutput?> AnalyzeAsync(CoachInput input, long? sessionId = null, CancellationToken ct = default)
    {
        for (var attempt = 0; attempt <= 1; attempt++)
        {
            var output = await AnalyzeOnceAsync(input, sessionId, ct);
            if (output != null)
                return output;
            if (ct.IsCancellationRequested)
                break; // 超时/取消不重试，直接降级
            if (attempt == 0)
                _logger.LogWarning("玩家辅导员AI首次调用失败，重试1次: SessionId={SessionId}", sessionId);
        }
        _logger.LogError("玩家辅导员AI重试后仍失败，回退原始目标: SessionId={SessionId}", sessionId);
        return null;
    }

    private async Task<CoachOutput?> AnalyzeOnceAsync(CoachInput input, long? sessionId, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        var aiRole = input.IsAdult ? "AdultCoach" : "Coach";
        var config = _modelFactory.GetModelConfig(aiRole);

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain(aiRole, $"开始分析玩家目标: {input.PlayerGoal}");

        try
        {
            // 成人轮切换成人版模板 + AdultCoach 模型，与正常轮同构二选一
            var templateName = input.IsAdult ? "player_coach_adult_system" : "player_coach_system";
            var systemPrompt = _promptService.LoadTemplate(templateName);

            // 构造上下文消息列表（按注意力权重排列，既定事实与玩家目标放最后）
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
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解当前世界状态。" });
            }

            if (!string.IsNullOrEmpty(input.MainQuestProgress))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[主线进度]\n{input.MainQuestProgress}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解主线进度。" });
            }

            if (!string.IsNullOrEmpty(input.PlayerInventory))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[玩家当前装备与道具]\n{input.PlayerInventory}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解玩家装备状态。" });
            }

            // 上轮辅导员状态（滚动评估的基准；首轮为空则本轮做冷启动消歧）
            if (!string.IsNullOrWhiteSpace(input.PrevCoachState))
            {
                messages.Add(new ChatMessage { Role = "user", Content = $"[上轮辅导员状态]\n{input.PrevCoachState}" });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解上轮目标进度，我将据此滚动更新。" });
            }

            // 本轮既定事实（世界增量，辅导员据此评估进度；GM没写的事等于没发生）
            messages.Add(new ChatMessage { Role = "user", Content = $"[本轮既定事实(GM输出)]\n{input.DirectorFacts}" });
            messages.Add(new ChatMessage { Role = "assistant", Content = "已了解本轮既定事实，我将据此评估目标进度。" });

            // 玩家目标（最末尾，优先注意力）
            messages.Add(new ChatMessage
            {
                Role = "user",
                Content = $"[玩家目标]\n{input.PlayerGoal}\n\n请输出辅导员分析JSON:"
            });

            var client = _modelFactory.CreateClient(config);
            var result = await client.ChatCompletionAsync(messages, config, ct, aiRole: aiRole);

            sw.Stop();
            await LogAiCallAsync(sessionId, config.ModelId, result, sw.ElapsedMilliseconds, input.IsAdult);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("玩家辅导员AI调用失败: {Error}", result.ErrorMessage);
                return null;
            }

            var output = ParseCoachOutput(result.Content);

            if (_modelFactory.IsDebugEnabled && output != null)
            {
                AiDebugLogger.LogCallChain(aiRole, $"目标状态: {output.GoalStatus}, 当前子目标: {output.CurrentSubgoal}");
                AiDebugLogger.LogCallChain(aiRole, $"next_focus数: {output.NextFocus?.Count ?? 0}");
            }

            return output;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "玩家辅导员AI服务异常");
            await LogAiCallAsync(sessionId, config.ModelId,
                new AiCompletionResult { IsSuccess = false, ErrorMessage = ex.Message },
                sw.ElapsedMilliseconds, input.IsAdult);
            return null;
        }
    }

    // ---- 状态映射与拼装（供协调器调用，无AI调用）----

    /// <summary>把辅导员输出映射为持久化状态（附带更新轮次）</summary>
    public static CoachState ToState(CoachOutput output, int round)
    {
        return new CoachState
        {
            ClarifiedIntent = output.ClarifiedIntent,
            CurrentSubgoal = output.CurrentSubgoal,
            ProgressNote = output.ProgressNote,
            GoalStatus = string.IsNullOrWhiteSpace(output.GoalStatus) ? CoachGoalStatus.Active : output.GoalStatus!,
            NextFocus = output.NextFocus,
            EntitiesInvolved = output.EntitiesInvolved,
            UpdatedRound = round
        };
    }

    /// <summary>序列化辅导员状态为JSON（落库用）</summary>
    public static string SerializeState(CoachState state) => JsonConvert.SerializeObject(state, _jsonSettings);

    /// <summary>反序列化持久化的辅导员状态（预计算DryRun复用next_focus用）；失败返回null</summary>
    public static CoachState? ParseState(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonConvert.DeserializeObject<CoachState>(json, _jsonSettings);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 拼装注入书记官的[辅导员策略指引]文本（真实轮由CoachOutput映射的state、预计算轮由复用state）。
    /// 返回null表示无有效指引（书记官回退原始PlayerGoal）。
    /// </summary>
    public static string? BuildCoachFocus(CoachState? state)
    {
        if (state == null) return null;

        var focuses = state.NextFocus?.Where(f => !string.IsNullOrWhiteSpace(f.Direction)).ToList();
        var hasFocus = focuses is { Count: > 0 };
        var hasSubgoal = !string.IsNullOrWhiteSpace(state.CurrentSubgoal);
        var hasIntent = !string.IsNullOrWhiteSpace(state.ClarifiedIntent);
        if (!hasFocus && !hasSubgoal && !hasIntent) return null;

        var sb = new StringBuilder();
        if (hasIntent) sb.AppendLine($"目标解读: {state.ClarifiedIntent}");
        if (hasSubgoal) sb.AppendLine($"当前子目标: {state.CurrentSubgoal}");
        if (!string.IsNullOrWhiteSpace(state.ProgressNote)) sb.AppendLine($"目标进度: {state.ProgressNote}");
        if (hasFocus)
        {
            sb.AppendLine("下一步策略方向(next_focus):");
            foreach (var f in focuses!)
            {
                sb.AppendLine(string.IsNullOrWhiteSpace(f.Rationale)
                    ? $"- {f.Direction}"
                    : $"- {f.Direction}（依据: {f.Rationale}）");
            }
        }

        var text = sb.ToString().TrimEnd();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private CoachOutput? ParseCoachOutput(string content)
    {
        try
        {
            var cleaned = CleanJsonContent(content);
            return JsonConvert.DeserializeObject<CoachOutput>(cleaned, _jsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("玩家辅导员输出解析失败: {Error}, 原始内容: {Content}", ex.Message, content);
            return null;
        }
    }

    private async Task LogAiCallAsync(long? sessionId, string modelName, AiCompletionResult result, long durationMs, bool isAdult = false)
    {
        try
        {
            var log = new GameAiCallLog
            {
                SessionId = sessionId,
                AiType = isAdult ? "adult_coach" : "coach",
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
