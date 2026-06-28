using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using DHY.Game.AI.Options;
using DHY.Game.AI.Utils;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Models;

/// <summary>
/// Poixe AI客户端（Chat Completions协议，用于成人内容叙事AI）
/// </summary>
public class PoixeClient : IAiModelClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GameAiOptions _options;
    private readonly AiModelConfig _modelConfig;
    private readonly ILogger<PoixeClient> _logger;

    private static readonly JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy() }
    };

    public PoixeClient(
        IHttpClientFactory httpClientFactory,
        GameAiOptions options,
        AiModelConfig modelConfig,
        ILogger<PoixeClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _modelConfig = modelConfig;
        _logger = logger;
    }

    /// <summary>
    /// 同步完整生成
    /// </summary>
    public async Task<AiCompletionResult> ChatCompletionAsync(
        List<ChatMessage> messages,
        AiModelConfig config,
        CancellationToken ct = default,
        string aiRole = "Unknown")
    {
        var debugEnabled = _options.EnableDebugLog;
        var sw = Stopwatch.StartNew();

        if (debugEnabled)
        {
            AiDebugLogger.LogRequest(aiRole, config.ModelId, messages.Count);
            AiDebugLogger.LogFullMessages(aiRole, messages);
        }

        var retries = 0;
        while (true)
        {
            try
            {
                var client = CreateHttpClient();
                var requestBody = BuildRequestBody(messages, config, stream: false);
                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody, _jsonSettings),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(
                    $"{GetBaseUrl()}/chat/completions", content, ct);

                var responseJson = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = $"Poixe API错误: {response.StatusCode} - {responseJson}";
                    _logger.LogWarning(errorMsg);

                    if (debugEnabled)
                        AiDebugLogger.LogError(aiRole, errorMsg);

                    if (retries < _options.MaxRetries)
                    {
                        retries++;
                        await Task.Delay(1000 * retries, ct);
                        continue;
                    }

                    sw.Stop();
                    return new AiCompletionResult
                    {
                        IsSuccess = false,
                        ErrorMessage = errorMsg
                    };
                }

                var result = ParseCompletionResponse(responseJson);
                sw.Stop();

                if (debugEnabled)
                {
                    AiDebugLogger.LogResponse(aiRole, result.Content,
                        result.InputTokens, result.OutputTokens, sw.ElapsedMilliseconds);
                }

                return result;
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Poixe调用异常");

                if (debugEnabled)
                    AiDebugLogger.LogError(aiRole, $"调用异常: {ex.Message}");

                if (retries < _options.MaxRetries)
                {
                    retries++;
                    await Task.Delay(1000 * retries, ct);
                    continue;
                }

                sw.Stop();
                return new AiCompletionResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"调用异常: {ex.Message}"
                };
            }
        }
    }

    /// <summary>
    /// 流式生成(SSE)
    /// </summary>
    public async IAsyncEnumerable<string> StreamChatCompletionAsync(
        List<ChatMessage> messages,
        AiModelConfig config,
        [EnumeratorCancellation] CancellationToken ct = default,
        string aiRole = "Unknown")
    {
        var debugEnabled = _options.EnableDebugLog;
        var sw = Stopwatch.StartNew();

        if (debugEnabled)
        {
            AiDebugLogger.LogStreamStart(aiRole, config.ModelId, messages.Count);
            AiDebugLogger.LogFullMessages(aiRole, messages);
        }

        var client = CreateHttpClient();
        var requestBody = BuildRequestBody(messages, config, stream: true);
        var content = new StringContent(
            JsonConvert.SerializeObject(requestBody, _jsonSettings),
            Encoding.UTF8,
            "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{GetBaseUrl()}/chat/completions")
        {
            Content = content
        };

        HttpResponseMessage? response = null;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Poixe流式调用异常");
            if (debugEnabled)
                AiDebugLogger.LogError(aiRole, $"流式调用异常: {ex.Message}");
            yield break;
        }

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        var fullContent = new System.Text.StringBuilder();

        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line))
                continue;

            if (!line.StartsWith("data: "))
                continue;

            var data = line["data: ".Length..];
            if (data == "[DONE]")
                break;

            var chunk = ParseStreamChunk(data);
            if (!string.IsNullOrEmpty(chunk))
            {
                if (debugEnabled)
                    AiDebugLogger.LogStreamChunk(chunk);
                fullContent.Append(chunk);
                yield return chunk;
            }
        }

        sw.Stop();
        if (debugEnabled)
            AiDebugLogger.LogStreamEnd(aiRole, sw.ElapsedMilliseconds, fullContent.ToString());
    }

    #region 私有方法

    private HttpClient CreateHttpClient()
    {
        var client = _httpClientFactory.CreateClient("Poixe");
        client.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds > 0 ? _options.TimeoutSeconds : 180);
        var apiKey = _modelConfig.ApiKey;
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }

    private string GetBaseUrl()
    {
        return string.IsNullOrWhiteSpace(_modelConfig.BaseUrl)
            ? "https://api.poixe.com/v1"
            : _modelConfig.BaseUrl.TrimEnd('/');
    }

    private static object BuildRequestBody(List<ChatMessage> messages, AiModelConfig config, bool stream)
    {
        // Chat Completions协议：所有消息（含system）放入messages数组
        var body = new Dictionary<string, object>
        {
            ["model"] = config.ModelId,
            ["messages"] = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray(),
            ["temperature"] = config.Temperature,
            ["stream"] = stream
        };

        // 思考模式：Chat Completions 通过 enable_thinking 控制
        if (config.EnableThinking)
        {
            body["enable_thinking"] = true;
        }

        return body;
    }

    private AiCompletionResult ParseCompletionResponse(string json)
    {
        try
        {
            var obj = JObject.Parse(json);

            // Chat Completions: choices[0].message.content
            var resultContent = "";
            var choices = obj["choices"] as JArray;
            if (choices != null && choices.Count > 0)
            {
                var firstChoice = choices[0];
                var msgContent = firstChoice?["message"]?["content"];
                if (msgContent != null)
                    resultContent = msgContent.Value<string>() ?? "";
            }

            // Chat Completions: usage.prompt_tokens / usage.completion_tokens
            var inputTokens = 0;
            var outputTokens = 0;
            var usage = obj["usage"];
            if (usage != null)
            {
                inputTokens = usage["prompt_tokens"]?.Value<int>() ?? 0;
                outputTokens = usage["completion_tokens"]?.Value<int>() ?? 0;
            }

            return new AiCompletionResult
            {
                Content = resultContent,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new AiCompletionResult
            {
                IsSuccess = false,
                ErrorMessage = $"响应解析失败: {ex.Message}"
            };
        }
    }

    private string? ParseStreamChunk(string json)
    {
        try
        {
            var obj = JObject.Parse(json);
            var choices = obj["choices"] as JArray;
            if (choices != null && choices.Count > 0)
            {
                var content = choices[0]?["delta"]?["content"];
                if (content != null)
                    return content.Value<string>();
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("流式chunk解析失败: {Error}", ex.Message);
        }

        return null;
    }

    #endregion
}
