using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using DHY.MG.Module.Sys.Dtos;

namespace DHY.MG.Module.Sys.Services
{
    /// <summary>
    /// DDBot Token记录服务
    /// 负责从AI响应中解析Token使用信息并记录
    /// </summary>
    /// <remarks>
    /// 应用服务: 协调Token解析和记录流程
    /// - 解析AI响应的usage信息
    /// - 调用DDBotTokenUsageService进行持久化
    /// - 异常时记录失败信息
    /// </remarks>
    public class DDBotTokenRecordingService : ITransient
    {
        private readonly DDBotTokenUsageService _tokenUsageService;
        private readonly DDBotOptions _options;

        public DDBotTokenRecordingService(
            DDBotTokenUsageService tokenUsageService,
            IOptions<DDBotOptions> options)
        {
            _tokenUsageService = tokenUsageService;
            _options = options.Value;
        }

        /// <summary>
        /// 从AI响应中解析token使用信息并记录
        /// </summary>
        /// <param name="responseBody">AI响应JSON字符串</param>
        /// <param name="modelName">模型名称</param>
        /// <param name="apiType">API类型(recognize/analyze)</param>
        /// <param name="processTimeMs">处理耗时(毫秒)</param>
        /// <param name="conversationName">会话名称(可选)</param>
        public async Task RecordTokenFromResponse(
            string responseBody,
            string modelName,
            string apiType,
            long processTimeMs,
            string? conversationName = null)
        {
            if (!_options.EnableTokenUsageRecording)
                return;

            try
            {
                var responseObj = JsonConvert.DeserializeObject<dynamic>(responseBody);
                
                int promptTokens = 0;
                int completionTokens = 0;
                int totalTokens = 0;
                bool isSuccess = true;
                string? errorMessage = null;

                // 解析usage信息
                if (responseObj?.usage != null)
                {
                    promptTokens = (int)responseObj.usage.prompt_tokens;
                    completionTokens = (int)responseObj.usage.completion_tokens;
                    totalTokens = (int)responseObj.usage.total_tokens;
                }

                // 记录token使用
                await _tokenUsageService.RecordTokenUsageAsync(new RecordTokenUsageInput
                {
                    ModelName = modelName,
                    ApiType = apiType,
                    PromptTokens = promptTokens,
                    CompletionTokens = completionTokens,
                    TotalTokens = totalTokens,
                    ProcessTimeMs = processTimeMs,
                    IsSuccess = isSuccess,
                    ErrorMessage = errorMessage,
                    ConversationName = conversationName
                });

                if (_options.EnableDebugLog)
                {
                    Debug.WriteLine($"[DDBot Token] 模型:{modelName}, API:{apiType}, " +
                                   $"Prompt:{promptTokens}, Completion:{completionTokens}, " +
                                   $"Total:{totalTokens}, 耗时:{processTimeMs}ms");
                }
            }
            catch (Exception ex)
            {
                // 解析失败时记录错误日志，但不影响主流程
                Debug.WriteLine($"[DDBot Token] 解析token信息失败: {ex.Message}");
                
                // 记录一次失败的调用
                try
                {
                    await _tokenUsageService.RecordTokenUsageAsync(new RecordTokenUsageInput
                    {
                        ModelName = modelName,
                        ApiType = apiType,
                        PromptTokens = 0,
                        CompletionTokens = 0,
                        TotalTokens = 0,
                        ProcessTimeMs = processTimeMs,
                        IsSuccess = false,
                        ErrorMessage = $"解析token失败: {ex.Message}",
                        ConversationName = conversationName
                    });
                }
                catch
                {
                    // 忽略记录失败的异常
                }
            }
        }

        /// <summary>
        /// 记录AI调用失败
        /// </summary>
        public async Task RecordTokenFailure(
            string modelName,
            string apiType,
            long processTimeMs,
            string errorMessage,
            string? conversationName = null)
        {
            if (!_options.EnableTokenUsageRecording)
                return;

            try
            {
                await _tokenUsageService.RecordTokenUsageAsync(new RecordTokenUsageInput
                {
                    ModelName = modelName,
                    ApiType = apiType,
                    PromptTokens = 0,
                    CompletionTokens = 0,
                    TotalTokens = 0,
                    ProcessTimeMs = processTimeMs,
                    IsSuccess = false,
                    ErrorMessage = errorMessage,
                    ConversationName = conversationName
                });
            }
            catch
            {
                // 忽略记录失败的异常
            }
        }
    }
}
