using DHY.DDCS.Module.Core.Options;
using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DHY.DDCS.Module.Core;

[AppStartup(100)]
public class StartUp : DDCSStartup
{
    protected override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
    }

    /// <summary>
    /// 客户端启动配置
    /// </summary>
    /// <param name="services"></param>
    protected override void ConfigureApplication(IServiceCollection services)
    {
        // 配置选项
        services.AddConfigurableOptions<BussinessBaseSettingOptions>();
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        // 配置选项
        services.AddConfigurableOptions<BussinessBaseSettingOptions>();
    }

}
