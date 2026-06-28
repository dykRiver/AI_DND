using Furion.ConfigurableOptions;
using Yitter.IdGenerator;

namespace DHY.Core;

/// <summary>
/// 雪花Id配置选项
/// </summary>
public sealed class SnowIdOptions : IdGeneratorOptions, IConfigurableOptions
{
    /// <summary>
    /// 缓存前缀
    /// </summary>
    public string WorkerPrefix { get; set; }
}