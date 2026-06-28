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
}
