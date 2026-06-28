using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Prompts;
using DHY.Game.AI.Utils;
using DHY.Game.Core.Entities;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 副本建筑师AI服务
/// </summary>
public class DungeonArchitectService : ITransient
{
    private readonly AiModelFactory _modelFactory;
    private readonly PromptTemplateService _promptService;
    private readonly SqlSugarRepository<GameAiCallLog> _aiLogRep;
    private readonly ILogger<DungeonArchitectService> _logger;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public DungeonArchitectService(
        AiModelFactory modelFactory,
        PromptTemplateService promptService,
        SqlSugarRepository<GameAiCallLog> aiLogRep,
        ILogger<DungeonArchitectService> logger)
    {
        _modelFactory = modelFactory;
        _promptService = promptService;
        _aiLogRep = aiLogRep;
        _logger = logger;
    }

    /// <summary>
    /// 生成完整副本内容
    /// </summary>
    /// <param name="template">副本模板</param>
    /// <param name="isReplay">是否为同题异卷重玩</param>
    public async Task<DungeonArchitectOutput?> GenerateDungeonAsync(GameDungeonTemplate template, bool isReplay = false)
    {
        var sw = Stopwatch.StartNew();
        var config = _modelFactory.GetModelConfig("Architect");

        if (_modelFactory.IsDebugEnabled)
            AiDebugLogger.LogCallChain("Architect", $"开始副本生成, 副本名: {template.Name}, 同题异卷: {isReplay}");

        try
        {
            var systemPrompt = _promptService.LoadTemplate("architect_system");

            // 构造模板信息
            var templateInfo = BuildTemplateInfo(template);

            // 渲染模板
            var renderedPrompt = _promptService.RenderTemplate(systemPrompt, new Dictionary<string, string>
            {
                { "dungeon_template", templateInfo }
            });

            // 若为同题异卷，注入额外指令
            if (isReplay)
            {
                renderedPrompt += "\n\n【重要】这是「同题异卷」模式：保留副本核心框架和目标，但变换具体细节：\n" +
                                  "- NPC的性格、外貌、口头禅需要显著变化\n" +
                                  "- 地点名称和具体描述需要变化\n" +
                                  "- 路径细节需要变化\n" +
                                  "- 时间线事件的具体表现需要变化\n" +
                                  "- 隐藏内容的触发条件需要变化";
            }

            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = renderedPrompt },
                new() { Role = "user", Content = "请基于以上副本模板,生成完整的副本内容JSON:" }
            };

            var client = _modelFactory.CreateClient();
            var result = await client.ChatCompletionAsync(messages, config, aiRole: "Architect");

            sw.Stop();
            await LogAiCallAsync(null, config.ModelId, result, sw.ElapsedMilliseconds);

            if (!result.IsSuccess)
            {
                _logger.LogWarning("建筑师AI调用失败: {Error}", result.ErrorMessage);
                return null;
            }

            var output = ParseArchitectOutput(result.Content);

            if (_modelFactory.IsDebugEnabled && output != null)
            {
                AiDebugLogger.LogCallChain("Architect", $"副本生成完成, NPC数: {output.Npcs?.Count ?? 0}, 主线: {output.MainQuest?.Objective}");
                AiDebugLogger.LogCallChain("Architect", $"世界设定: {output.WorldSetting?.Era} | {output.WorldSetting?.Geography}");
            }

            return output;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "建筑师AI服务异常");
            await LogAiCallAsync(null, config.ModelId,
                new AiCompletionResult { IsSuccess = false, ErrorMessage = ex.Message },
                sw.ElapsedMilliseconds);
            return null;
        }
    }

    /// <summary>
    /// 将NPC数据转为GameNpcProfile实体
    /// </summary>
    public List<GameNpcProfile> ConvertToNpcProfiles(long sessionId, List<NpcData> npcs)
    {
        return npcs.Select(n => new GameNpcProfile
        {
            SessionId = sessionId,
            NpcIdentifier = n.NpcId,
            Name = n.Name,
            Role = n.Role,
            Personality = n.Personality,
            LanguageStyle = n.LanguageStyle,
            Catchphrase = n.Catchphrase,
            InitialAttitude = n.InitialAttitude,
            CurrentAttitude = n.InitialAttitude,
            Location = n.Location,
            ActionPlan = n.ActionPlan,
            IsAlive = true,
            IsCritical = true
        }).ToList();
    }

    private static string BuildTemplateInfo(GameDungeonTemplate template)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"副本名称: {template.Name}");
        sb.AppendLine($"世界观主题: {template.WorldTheme}");
        sb.AppendLine($"难度等级: {template.Difficulty}");
        sb.AppendLine($"时间限制: {template.TimeLimitDays}天");

        if (template.Tags is { Count: > 0 })
            sb.AppendLine($"关键词标签: {string.Join(", ", template.Tags)}");

        if (!string.IsNullOrEmpty(template.Description))
            sb.AppendLine($"副本描述: {template.Description}");

        if (!string.IsNullOrEmpty(template.BasePrompt))
            sb.AppendLine($"补充设定:\n{template.BasePrompt}");

        return sb.ToString();
    }

    private DungeonArchitectOutput? ParseArchitectOutput(string content)
    {
        try
        {
            var cleaned = CleanJsonContent(content);
            return JsonConvert.DeserializeObject<DungeonArchitectOutput>(cleaned, _jsonSettings);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("建筑师输出解析失败: {Error}, 内容片段: {Content}",
                ex.Message, content.Length > 200 ? content[..200] : content);
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
                AiType = "architect",
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
