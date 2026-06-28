using Furion.ConfigurableOptions;

namespace DHY.MG.Module.Sys.Dtos
{
    /// <summary>
    /// 阿里云配置选项
    /// </summary>
    public class AliYunOptions : IConfigurableOptions
    {
        /// <summary>
        /// 阿里云百炼DashScope API Key
        /// </summary>
        public string DashScopeApiKey { get; set; }

        /// <summary>
        /// 阿里云百炼DashScope Endpoint
        /// </summary>
        public string DashScopeEndpoint { get; set; }

        /// <summary>
        /// 千义星辰 API Key
        /// </summary>
        public string QianYiXingChenApiKey { get; set; }

        /// <summary>
        /// 千义星辰 Endpoint
        /// </summary>
        public string QianYiXingChenEndpoint { get; set; }

        /// <summary>
        /// 默认超时时间（秒）
        /// 用于AI服务调用的默认超时配置
        /// 建议范围：10-60秒，默认30秒
        /// </summary>
        public int DefaultTimeout { get; set; } = 30;
    }
}
