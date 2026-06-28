using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Prompts;
using DHY.Game.AI.Utils;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 行动分类器服务
/// </summary>
public class ActionClassifierService : ITransient
{
    private readonly AiModelFactory _modelFactory;
    private readonly PromptTemplateService _promptService;
    private readonly ILogger<ActionClassifierService> _logger;

    public ActionClassifierService(
        AiModelFactory modelFactory,
        PromptTemplateService promptService,
        ILogger<ActionClassifierService> logger)
    {
        _modelFactory = modelFactory;
        _promptService = promptService;
        _logger = logger;
    }

    /// <summary>
    /// 分类玩家输入（含可行性判定 + 技能判定）
    /// </summary>
    /// <param name="playerInput">玩家输入文本</param>
    /// <param name="currentState">当前状态描述</param>
    /// <param name="playerInventory">玩家背包摘要（供可行性判定）</param>
    /// <param name="npcProfiles">NPC档案摘要（供优劣势判定）</param>
    /// <returns>分类结果</returns>
    public async Task<ClassificationResult> ClassifyAsync(string playerInput, string currentState, string playerInventory = "", string npcProfiles = "")
    {
        try
        {
            if (_modelFactory.IsDebugEnabled)
                AiDebugLogger.LogCallChain("Classifier", $"开始分类玩家输入: {playerInput}");

            var systemPrompt = _promptService.LoadTemplate("classifier_system");
            var inventoryText = string.IsNullOrEmpty(playerInventory) ? "（无道具）" : playerInventory;
            var npcText = string.IsNullOrEmpty(npcProfiles) ? "（无NPC）" : npcProfiles;
            var userContent = $"当前场景状态: {currentState}\n\nNPC状态:\n{npcText}\n\n玩家背包: {inventoryText}\n\n玩家输入: {playerInput}";

            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userContent }
            };

            var config = _modelFactory.GetModelConfig("Classifier");
            var client = _modelFactory.CreateClient();
            var result = await client.ChatCompletionAsync(messages, config, aiRole: "Classifier");

            if (!result.IsSuccess)
            {
                _logger.LogWarning("行动分类AI调用失败: {Error}", result.ErrorMessage);
                return new ClassificationResult { IsRoutine = false, Confidence = 0 };
            }

            var classificationResult = ParseClassificationResult(result.Content);

            if (_modelFactory.IsDebugEnabled)
            {
                var j = classificationResult.Judgment;
                var judgmentDetail = j != null
                    ? $"Judgment(needed={j.Needed}, skill={j.Skill}, dc={j.Dc}, advantage={j.Advantage}, disadvantage={j.Disadvantage}, context={j.Context})"
                    : "Judgment=null";
                AiDebugLogger.LogCallChain("Classifier", $"分类结果: IsRoutine={classificationResult.IsRoutine}, NeedsStateChange={classificationResult.NeedsStateChange}, IsFeasible={classificationResult.IsFeasible}, IsAdult={classificationResult.IsAdult}, ActionIntent={classificationResult.ActionIntent}, Confidence={classificationResult.Confidence}, Reason={classificationResult.Reason}, {judgmentDetail}");
            }

            return classificationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "行动分类异常");
            // 失败默认返回非常规（交给导演处理）
            return new ClassificationResult { IsRoutine = false, Confidence = 0 };
        }
    }

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    private ClassificationResult ParseClassificationResult(string content)
    {
        try
        {
            var cleaned = CleanJsonContent(content);
            var root = JObject.Parse(cleaned);

            // 解析judgment字段（非常规行动时输出）
            JudgmentInfo? judgment = null;
            var judgmentToken = root["judgment"];
            if (judgmentToken != null && judgmentToken.Type == JTokenType.Object)
            {
                judgment = judgmentToken.ToObject<JudgmentInfo>(JsonSerializer.Create(_jsonSettings));
            }

            return new ClassificationResult
            {
                IsRoutine = root["is_routine"]?.Value<bool>() ?? false,
                Confidence = root["confidence"]?.Value<double>() ?? 0.5,
                Reason = root["reason"]?.Value<string>(),
                IsFeasible = root["is_feasible"]?.Value<bool>() ?? true,
                InfeasibleReason = root["infeasible_reason"]?.Value<string>(),
                NeedsStateChange = root["needs_state_change"]?.Value<bool>() ?? false,
                IsAdult = root["is_adult"]?.Value<bool>() ?? false,
                ActionIntent = root["action_intent"]?.Value<string>(),
                Judgment = judgment
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning("分类结果解析失败: {Error}, 原始内容: {Content}", ex.Message, content);
            return new ClassificationResult { IsRoutine = false, Confidence = 0 };
        }
    }

    /// <summary>
    /// 清理AI输出中可能的非标准JSON（去掉markdown代码块标记等）
    /// </summary>
    private static string CleanJsonContent(string content)
    {
        content = content.Trim();

        // 去掉 ```json ... ``` 包裹
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
