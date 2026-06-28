using Furion.ConfigurableOptions;
using Microsoft.Extensions.Configuration;

namespace DHY.Core.Option.RemoteRequest
{
    /// <summary>
    /// HTTP客户端配置
    /// </summary>
    public class HttpClientSettingOptions : IConfigurableOptions<HttpClientSettingOptions>
    {
        public List<HttpClientSetting> Clients { get; set; }

        public void PostConfigure(HttpClientSettingOptions options, IConfiguration configuration)
        {
            //throw new NotImplementedException();
        }
    }
}
