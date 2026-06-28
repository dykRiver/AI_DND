namespace DHY.Game.AI.Models;

/// <summary>
/// 统一AI调用接口
/// </summary>
public interface IAiModelClient
{
    /// <summary>
    /// 同步完整生成
    /// </summary>
    Task<AiCompletionResult> ChatCompletionAsync(
        List<ChatMessage> messages,
        AiModelConfig config,
        CancellationToken ct = default,
        string aiRole = "Unknown");

    /// <summary>
    /// 流式生成(SSE)
    /// </summary>
    IAsyncEnumerable<string> StreamChatCompletionAsync(
        List<ChatMessage> messages,
        AiModelConfig config,
        CancellationToken ct = default,
        string aiRole = "Unknown");
}

/// <summary>
/// 聊天消息
/// </summary>
public class ChatMessage
{
    /// <summary>角色: system/user/assistant</summary>
    public string Role { get; set; } = "";

    /// <summary>消息内容</summary>
    public string Content { get; set; } = "";
}

/// <summary>
/// AI调用结果
/// </summary>
public class AiCompletionResult
{
    /// <summary>生成内容</summary>
    public string Content { get; set; } = "";

    /// <summary>输入Token数</summary>
    public int InputTokens { get; set; }

    /// <summary>输出Token数</summary>
    public int OutputTokens { get; set; }

    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}
