using DHY.Game.AI.Options;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Models;

/// <summary>
/// AI模型工厂
/// </summary>
public class AiModelFactory : ITransient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GameAiOptions _options;
    private readonly ILogger<DashScopeClient> _dashScopeLogger;
    private readonly ILogger<PoixeClient> _poixeLogger;

    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    public bool IsDebugEnabled => _options.EnableDebugLog;

    public AiModelFactory(
        IHttpClientFactory httpClientFactory,
        IOptions<GameAiOptions> options,
        ILogger<DashScopeClient> dashScopeLogger,
        ILogger<PoixeClient> poixeLogger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _dashScopeLogger = dashScopeLogger;
        _poixeLogger = poixeLogger;
    }

    /// <summary>
    /// 创建AI模型客户端（默认DashScope）
    /// </summary>
    public IAiModelClient CreateClient()
    {
        return new DashScopeClient(_httpClientFactory, _options, _dashScopeLogger);
    }

    /// <summary>
    /// 根据模型配置创建对应Provider的客户端
    /// </summary>
    public IAiModelClient CreateClient(AiModelConfig config)
    {
        if (config.Provider?.Equals("poixe", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new PoixeClient(_httpClientFactory, _options, config, _poixeLogger);
        }
        return new DashScopeClient(_httpClientFactory, _options, _dashScopeLogger);
    }

    /// <summary>
    /// 获取指定类型的模型配置
    /// </summary>
    public AiModelConfig GetModelConfig(string modelType)
    {
        if (_options.Models != null && _options.Models.TryGetValue(modelType, out var config))
            return config;

        // 默认配置
        return new AiModelConfig
        {
            ModelId = "qwen-plus",
            Temperature = 0.7,
            EnableThinking = true
        };
    }
}
