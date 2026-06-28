using Furion.ConfigurableOptions;

namespace DHY.DDCS.Module.Core.Options
{
    /// <summary>
    /// 业务基础设置
    /// </summary>
    public sealed class BussinessBaseSettingOptions : IConfigurableOptions
    {
        /// <summary>
        /// 运行模式；1自动（默认），2手动。
        /// </summary>
        public int RunMode { get; set; }

    }
}
