using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Prompts;
using DHY.Game.AI.Utils;
using DHY.Game.Core.Logging;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 导演AI服务
/// </summary>
public class DirectorAiService : ITransient
{
    private readonly AiModelFactory _modelFactory;
    private readonly PromptTemplateService _promptService;
    private readonly SqlSugarRepository<GameAiCallLog> _aiLogRep;
    private readonly ILogger<DirectorAiService> _logger;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public DirectorAiService(
        AiModelFactory modelFactory,
        PromptTemplateService promptService,
        SqlSugarRepository<GameAiCallLog> aiLogRep,
        ILogger<DirectorAiService> logger)
    {
        _modelFactory = modelFactory;
        _promptService = promptService;
        _aiLogRep = aiLogRep;
        _logger = logger;
    }

    /// <summary>
    /// 导演AI推演
    /// </summary>
    public async Task<DirectorOutput?> DirectAsync(DirectorInput input, long? sessionId = null)
    {
        var sw = Stopwatch.StartNew();
        var config = _modelFactory.GetModelConfig("Director");

        if (_modelFactory.IsDebugEnabled)
        {
            AiDebugLogger.LogCallChain("Director", $"开始导演AI推演, 玩家行动: {input.PlayerAction}");
            // 控制台截断100字符，文件写完整世界状态
            AiDebugLogger.LogCallChain("Director", $"世界状态摘要: {(input.WorldState?.Length > 100 ? input.WorldState[..100] + "..." : input.WorldState)}");
            GameFileLogger.Write("[AI链路][Director]", $"世界状态(完整): {input.WorldState}");
        }

        try
        {
            // 前台导演模板：只输出叙事推演字段，状态记账已拆给书记官(ScribeAiService)
            var systemPrompt = _promptService.LoadTemplate("director_front_system");

            // 构造上下文消息列表（按注意力权重排列，关键信息放最后）
            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt }
            };

            // 玩家角色名称（让导演AI在蓝图中正确引用）
            if (!string.IsNullOrEmpty(input.CharacterName))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[玩家角色名称]\n{input.CharacterName}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解玩家角色名称。" });
            }

            // 世界设定（不变信息）
            if (!string.IsNullOrEmpty(input.DungeonContext))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[世界设定]\n{input.DungeonContext}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解世界设定。" });
            }

            // NPC档案卡
            if (!string.IsNullOrEmpty(input.NpcProfiles))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[NPC档案]\n{input.NpcProfiles}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解NPC信息。" });
            }

            // 当前世界状态快照
            if (!string.IsNullOrEmpty(input.WorldState))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[当前世界状态(含历史)]\n{input.WorldState}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解当前状态。" });
            }

            // 主线进度
            if (!string.IsNullOrEmpty(input.MainQuestProgress))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[主线进度]\n{input.MainQuestProgress}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解主线进度。" });
            }

            // 支线任务清单（含触发条件，供导演埋线索/制造触发契机；完成标记归书记官）
            if (!string.IsNullOrEmpty(input.SideQuestList))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[支线任务清单]\n{input.SideQuestList}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解支线任务清单。" });
            }

            // 隐藏内容清单（含触发条件，供导演埋线索；发现标记归书记官）
            if (!string.IsNullOrEmpty(input.HiddenContentList))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[隐藏内容清单]\n{input.HiddenContentList}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解隐藏内容清单。" });
            }

            // 角色再定位片段
            if (!string.IsNullOrEmpty(input.RepositionSnippet))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[角色再定位]\n{input.RepositionSnippet}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已完成再定位。" });
            }

            // 玩家背包状态（每次交互动态注入）
            if (!string.IsNullOrEmpty(input.PlayerInventory))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[玩家当前装备与道具]\n{input.PlayerInventory}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解玩家装备状态。" });
            }

            // 判定结果（由代码层掷骰后注入）
            if (!string.IsNullOrEmpty(input.JudgmentOutcome))
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = $"[\u5224\u5b9a\u7ed3\u679c]\n{input.JudgmentOutcome}"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "已了解判定结果。" });
            }

            // 停滞检测提示（在玩家行动消息之前注入）
            if (input.IsStagnant)
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = "[剧情推进提示] 最近数轮世界状态变化较小，玩家可能迷失方向。请在本轮主动引入推进线索：通过NPC主动行为、环境变化、或narrative_hooks引导玩家关注主线方向。"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "明白，我会在本轮主动引入推进线索。" });
            }

            // 推进型行动提示（玩家点选了粗粒度剧情推进选项，在玩家行动消息之前注入）
            if (input.IsAdvanceAction)
            {
                messages.Add(new ChatMessage
                {
                    Role = "user",
                    Content = "[推进型行动] 玩家选择的是一个粗粒度剧情推进方向，覆盖一段旅程/一个时段/一整段事件。本轮必须 beat_scale=chapter 并输出 beats 分镜表，一次性推演完整段推进，不得只推演第一步就收尾。"
                });
                messages.Add(new ChatMessage { Role = "assistant", Content = "明白，本轮我将以章节档一次性推演完这整段推进，并输出 beats 分镜表。" });
            }

            // 玩家本次行动（最末尾，优先注意力）
            var actionPrefix = input.IsAdvanceAction ? "[推进型行动] " : "";
            var actionContent = !string.IsNullOrEmpty(input.ActionIntent)
                ? $"{actionPrefix}玩家行动意图: {input.ActionIntent}\n玩家原始表达: {input.PlayerAction}"
                : $"{actionPrefix}玩家行动: {input.PlayerAction}";
            messages.Add(new ChatMessage
            {
                Role = "user",
                Content = $"{actionContent}\n\n请输出导演推演JSON:"
            });

            var client = _modelFactory.CreateClient();
            var result = await client.ChatCompletionAsync(messages, config, aiRole: "Director");

            sw.Stop();

            // 记录AI调用日志
            await LogAiCallAsync(sessionId, config.ModelId, result, sw.ElapsedMilliseconds);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("导演AI调用失败: {Error}", result.ErrorMessage);
                return null;
            }

            var directorOutput = ParseDirectorOutput(result.Content);

            if (_modelFactory.IsDebugEnabled && directorOutput != null)
            {
                AiDebugLogger.LogCallChain("Director", $"叙事种子: {(directorOutput.NarrativeSeed?.Length > 80 ? directorOutput.NarrativeSeed[..80] + "..." : directorOutput.NarrativeSeed)}");
                AiDebugLogger.LogCallChain("Director", $"NPC行动数: {directorOutput.NpcActions?.Count ?? 0}");
                if (directorOutput.Pacing != null)
                    AiDebugLogger.LogCallChain("Director", $"节奏: 紧张度={directorOutput.Pacing.TensionLevel}, 备注={directorOutput.Pacing.Note}");
                if (!string.IsNullOrEmpty(directorOutput.ProseGuidance))
                    AiDebugLogger.LogCallChain("Director", $"文风指导: {directorOutput.ProseGuidance}");
            }

            return directorOutput;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "导演AI服务异常");
            await LogAiCallAsync(sessionId, config.ModelId, 
                new AiCompletionResult { IsSuccess = false, ErrorMessage = ex.Message }, 
                sw.ElapsedMilliseconds);
            return null;
        }
    }

    private DirectorOutput? ParseDirectorOutput(string content)
    {
        var cleaned = CleanJsonContent(content);
        try
        {
            var output = JsonConvert.DeserializeObject<DirectorOutput>(cleaned, _jsonSettings);
            if (output != null && string.IsNullOrEmpty(output.NarrativeSeed))
            {
                _logger.LogWarning("导演输出解析成功但NarrativeSeed为空，疑似JSON字段不匹配。原始内容前200字: {Content}",
                    content.Length > 200 ? content[..200] : content);
            }
            return output;
        }
        catch (Exception ex)
        {
            // 常见损坏：模型在字符串值内嵌入未转义的ASCII双引号（如对话引语）破坏JSON结构，修复后重试
            _logger.LogWarning("导演输出解析失败，尝试引号修复: {Error}", ex.Message);
        }

        try
        {
            var repaired = RepairUnescapedQuotes(cleaned);
            var output = JsonConvert.DeserializeObject<DirectorOutput>(repaired, _jsonSettings);
            if (output != null)
                _logger.LogWarning("导演输出引号修复后解析成功（模型输出含未转义双引号）");
            return output;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("导演输出修复后仍解析失败: {Error}, 原始内容: {Content}", ex.Message, content);
            return null;
        }
    }

    /// <summary>
    /// 启发式修复JSON字符串值内未转义的ASCII双引号：
    /// 逐字符扫描跟踪是否处于字符串内；字符串内遇到双引号时，若其后首个非空字符为结构字符(: , } ]或结尾)则视为字符串结束引号，
    /// 否则视为内容内嵌引号并转义为\"。仅作为解析失败后的兜底重试手段。
    /// </summary>
    private static string RepairUnescapedQuotes(string json)
    {
        var sb = new System.Text.StringBuilder(json.Length + 16);
        var inString = false;
        for (var i = 0; i < json.Length; i++)
        {
            var c = json[i];
            if (!inString)
            {
                if (c == '"') inString = true;
                sb.Append(c);
                continue;
            }

            if (c == '\\')
            {
                // 已有转义序列：原样复制（含反斜杠后一个字符）
                sb.Append(c);
                if (i + 1 < json.Length)
                {
                    sb.Append(json[i + 1]);
                    i++;
                }
                continue;
            }

            if (c == '"')
            {
                // 前瞻判定：结束引号 or 内嵌引号
                var j = i + 1;
                while (j < json.Length && char.IsWhiteSpace(json[j])) j++;
                if (j >= json.Length || json[j] is ':' or ',' or '}' or ']')
                {
                    inString = false;
                    sb.Append(c);
                }
                else
                {
                    sb.Append('\\').Append(c);
                }
                continue;
            }

            sb.Append(c);
        }
        return sb.ToString();
    }

    private async Task LogAiCallAsync(long? sessionId, string modelName, AiCompletionResult result, long durationMs)
    {
        try
        {
            var log = new GameAiCallLog
            {
                SessionId = sessionId,
                AiType = "director",
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
