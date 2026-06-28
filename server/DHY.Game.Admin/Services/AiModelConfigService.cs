using DHY.Game.Admin.Dtos;
using DHY.Game.AI.Options;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DHY.Game.Admin.Services;

/// <summary>
/// AI模型配置管理服务
/// </summary>
[ApiDescriptionSettings("GameAdmin")]
public class AiModelConfigService : IDynamicApiController, ITransient
{
    private readonly IOptionsMonitor<GameAiOptions> _aiOptions;

    public AiModelConfigService(IOptionsMonitor<GameAiOptions> aiOptions)
    {
        _aiOptions = aiOptions;
    }

    /// <summary>
    /// 获取所有AI模型配置列表
    /// </summary>
    [DisplayName("获取AI模型配置列表")]
    [ApiDescriptionSettings(Name = "GetModelConfigs"), HttpGet]
    public List<ModelConfigOutput> GetModelConfigs()
    {
        var options = _aiOptions.CurrentValue;
        var result = new List<ModelConfigOutput>();

        if (options.Models != null)
        {
            foreach (var kv in options.Models)
            {
                result.Add(new ModelConfigOutput
                {
                    AiRole = kv.Key,
                    ModelId = kv.Value.ModelId,
                    Temperature = kv.Value.Temperature,
                    EnableThinking = kv.Value.EnableThinking,
                    BaseUrl = kv.Value.BaseUrl,
                    ApiKeyMasked = MaskApiKey(kv.Value.ApiKey)
                });
            }
        }

        return result;
    }

    /// <summary>
    /// 更新指定AI角色的模型配置
    /// </summary>
    [DisplayName("更新AI模型配置")]
    [ApiDescriptionSettings(Name = "UpdateModelConfig"), HttpPost]
    public async Task UpdateModelConfigAsync(UpdateModelConfigInput input)
    {
        // 输入验证
        if (input.Temperature < 0 || input.Temperature > 2)
            throw Oops.Oh("Temperature 必须在 0-2 之间");

        var validRoles = new[] { "Classifier", "Director", "Narrative", "Architect" };
        if (!validRoles.Contains(input.AiRole))
            throw Oops.Oh($"无效的AI角色: {input.AiRole}，有效值: {string.Join("/", validRoles)}");

        // 读取配置文件并更新
        var configPath = Path.Combine(AppContext.BaseDirectory, "Configuration", "GameAiOptions.json");
        if (!File.Exists(configPath))
            configPath = Path.Combine(Directory.GetCurrentDirectory(), "Configuration", "GameAiOptions.json");

        var jsonContent = await File.ReadAllTextAsync(configPath);
        var optionsNode = JObject.Parse(jsonContent);
        var modelsNode = optionsNode["GameAi"]?["Models"];
        if (modelsNode != null)
        {
            var modelNode = modelsNode[input.AiRole] as JObject;
            if (modelNode != null)
            {
                // 只更新传入的字段，保留BaseUrl/ApiKey等现有配置
                modelNode["ModelId"] = input.ModelId;
                modelNode["Temperature"] = input.Temperature;
                modelNode["EnableThinking"] = input.EnableThinking;
            }
            else
            {
                modelsNode[input.AiRole] = JObject.FromObject(new
                {
                    ModelId = input.ModelId,
                    Temperature = input.Temperature,
                    EnableThinking = input.EnableThinking
                });
            }
        }

        var updatedJson = optionsNode.ToString(Formatting.Indented);

        if (!string.IsNullOrEmpty(updatedJson))
        {
            await File.WriteAllTextAsync(configPath, updatedJson);
        }
    }

    /// <summary>
    /// 测试AI模型连通性
    /// </summary>
    [DisplayName("测试AI连通性")]
    [ApiDescriptionSettings(Name = "TestConnection"), HttpPost]
    public async Task<ConnectionTestResult> TestConnectionAsync(TestConnectionInput input)
    {
        var options = _aiOptions.CurrentValue;
        var result = new ConnectionTestResult();

        if (options.Models == null || !options.Models.ContainsKey(input.AiRole))
        {
            result.IsSuccess = false;
            result.ErrorMessage = $"未找到AI角色 {input.AiRole} 的配置";
            return result;
        }

        var modelConfig = options.Models[input.AiRole];
        var sw = Stopwatch.StartNew();

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds > 0 ? options.TimeoutSeconds : 30);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {modelConfig.ApiKey}");

            var requestBody = new Dictionary<string, object>
            {
                ["model"] = modelConfig.ModelId,
                ["messages"] = new[] { new { role = "user", content = "hello" } },
                ["max_tokens"] = 10
            };

            if (modelConfig.EnableThinking)
            {
                requestBody["enable_thinking"] = true;
            }

            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                System.Text.Encoding.UTF8,
                "application/json");

            var baseUrl = string.IsNullOrWhiteSpace(modelConfig.BaseUrl)
                ? "https://dashscope.aliyuncs.com/compatible-mode/v1"
                : modelConfig.BaseUrl.TrimEnd('/');

            var response = await httpClient.PostAsync(
                $"{baseUrl}/chat/completions", content);

            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            result.IsSuccess = response.IsSuccessStatusCode;

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {errorBody}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            result.IsSuccess = false;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    /// <summary>
    /// 测试Poixe模型（Chat Completions协议，完整请求/响应日志）
    /// </summary>
    [DisplayName("测试Poixe模型")]
    [ApiDescriptionSettings(Name = "TestPoixe"), HttpPost]
    public async Task<TestPoixeResult> TestPoixeAsync(TestPoixeInput input)
    {
        var options = _aiOptions.CurrentValue;
        var result = new TestPoixeResult();

        // 读取AdultNarrative配置，或使用用户传入的值覆盖
        var config = options.Models != null && options.Models.TryGetValue("AdultNarrative", out var c) ? c : null;
        if (config == null)
        {
            result.ErrorMessage = "未找到AdultNarrative模型配置";
            return result;
        }

        var modelId = input.ModelId ?? config.ModelId;
        var temperature = input.Temperature ?? config.Temperature;
        var enableThinking = input.EnableThinking ?? config.EnableThinking;
        var baseUrl = config.BaseUrl?.TrimEnd('/') ?? "https://api.poixe.com/v1";
        var apiKey = config.ApiKey;

        // Chat Completions协议：messages数组（含system + user）
        var messages = new List<object>
        {
            new { role = "system", content = "You are a helpful assistant." },
            new { role = "user", content = input.Prompt }
        };

        var body = new Dictionary<string, object>
        {
            ["model"] = modelId,
            ["messages"] = messages,
            ["temperature"] = temperature,
            ["stream"] = input.Stream
        };

        // 仅在显式启用时附加 enable_thinking（Grok等非OpenAI模型可能不支持）
        if (enableThinking)
        {
            body["enable_thinking"] = true;
        }

        var requestJson = JsonConvert.SerializeObject(body, Formatting.Indented);
        result.RequestBody = requestJson;

        var sw = Stopwatch.StartNew();
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(180);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{baseUrl}/chat/completions", content);

            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            result.StatusCode = (int)response.StatusCode;
            result.IsSuccess = response.IsSuccessStatusCode;

            if (response.IsSuccessStatusCode)
            {
                if (input.Stream)
                {
                    // ===== 流式模式：逐行读取SSE =====
                    var responseText = await response.Content.ReadAsStringAsync();
                    result.ResponseBody = responseText;

                    var fullContent = new System.Text.StringBuilder();
                    var thinkingContent = new System.Text.StringBuilder();
                    var lines = responseText.Split('\n');
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.Trim();
                        if (!line.StartsWith("data: ")) continue;
                        var jsonStr = line.Substring(6).Trim();
                        if (jsonStr == "[DONE]") break;

                        try
                        {
                            var chunk = JObject.Parse(jsonStr);
                            var delta = chunk["choices"]?[0]?["delta"];
                            if (delta == null) continue;

                            // 提取正文内容
                            var text = delta["content"]?.Value<string>();
                            if (!string.IsNullOrEmpty(text))
                                fullContent.Append(text);

                            // 提取思考/推理内容（reasoning_content / thinking_content）
                            var reasoning = delta["reasoning_content"]?.Value<string>()
                                         ?? delta["thinking_content"]?.Value<string>();
                            if (!string.IsNullOrEmpty(reasoning))
                                thinkingContent.Append(reasoning);
                        }
                        catch { /* 忽略解析失败的行 */ }
                    }

                    result.AiReply = fullContent.Length > 0
                        ? fullContent.ToString()
                        : "(流式响应中未解析到content，请查看原始响应)";

                    if (thinkingContent.Length > 0)
                        result.ThinkingContent = thinkingContent.ToString();
                }
                else
                {
                    // ===== 非流式模式：标准JSON =====
                    var responseText = await response.Content.ReadAsStringAsync();
                    result.ResponseBody = responseText;

                    try
                    {
                        var respObj = JObject.Parse(responseText);
                        var text = respObj["choices"]?[0]?["message"]?["content"]?.Value<string>();
                        result.AiReply = text ?? "(解析choices失败，请查看原始响应)";
                    }
                    catch { result.AiReply = "(解析响应失败，请查看原始响应)"; }
                }
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                result.ResponseBody = errorBody;
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {errorBody}";
            }
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.LatencyMs = (int)sw.ElapsedMilliseconds;
            result.IsSuccess = false;
            result.ErrorMessage = $"异常: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// 获取可用模型列表
    /// </summary>
    [DisplayName("获取可用模型列表")]
    [ApiDescriptionSettings(Name = "GetAvailableModels"), HttpGet]
    public List<string> GetAvailableModels()
    {
        return new List<string>
        {
            "qwen-turbo",
            "qwen-plus",
            "qwen-max",
            "qwen-max-longcontext",
            "qwen-long",
            "qwen3-turbo",
            "qwen3-plus",
            "qwen3-max",
            "qwen2.5-72b-instruct",
            "qwen2.5-32b-instruct"
        };
    }

    #region 辅助方法

    private static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "未配置";
        if (apiKey.Length <= 4)
            return "****";
        return apiKey[..4] + new string('*', apiKey.Length - 4);
    }

    #endregion
}
