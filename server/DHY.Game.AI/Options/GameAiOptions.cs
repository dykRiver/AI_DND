namespace DHY.Game.AI.Options;

/// <summary>
/// 游戏AI配置选项
/// </summary>
public class GameAiOptions : IConfigurableOptions
{
    /// <summary>
    /// AI模型配置字典
    /// </summary>
    public Dictionary<string, AiModelConfig> Models { get; set; }

    /// <summary>
    /// 超时时间(秒)
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// 最大重试次数
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// 是否启用调试日志（控制台实时输出AI调用链路和流式文字）
    /// </summary>
    public bool EnableDebugLog { get; set; }

    /// <summary>
    /// 章节档预取分镜段数（预计算时预生成的分镜数，默认1）。
    /// 点选后先秒回放已预取的前 N 段，其余分镜在玩家阅读期间实时续写，掩盖生成延迟。
    /// 0 或未配置时按 1 处理。
    /// </summary>
    public int ChapterPrefetchBeats { get; set; }
}

/// <summary>
/// AI模型配置
/// </summary>
public class AiModelConfig
{
    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 温度参数
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// 是否启用思考模式（深度推理）
    /// 开启后AI先推理再输出，质量更高但响应更慢、Token消耗更多
    /// </summary>
    public bool EnableThinking { get; set; }

    /// <summary>
    /// AI提供商（dashscope/poixe，默认dashscope）
    /// </summary>
    public string Provider { get; set; } = "dashscope";

    /// <summary>
    /// API基础URL（每个模型独立配置）
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// API密钥（每个模型独立配置）
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// 最大输出Token数（0=不设置，使用服务商默认上限）。
    /// 章节档分段生成等长文场景应显式调高，避免单段被截断。
    /// </summary>
    public int MaxTokens { get; set; }

    /// <summary>
    /// 推理强度档位（reasoning_effort），OpenAI兼容接口的顶层参数。
    /// 仅对 DashScope 的 deepseek-v4 系列思考模式生效。
    /// 可选值：high / max（deepseek-v4-flash 不支持 low；low 仅 deepseek-v4.1-flash、
    /// deepseek-v4-flash-0731、deepseek-v4-pro-0813 支持）。
    /// 默认策略：留空/null = 不传该参数，使用平台默认档位（high）。
    /// 仅在 EnableThinking=true 时才会附加到请求体。
    /// </summary>
    public string? ReasoningEffort { get; set; }
}
