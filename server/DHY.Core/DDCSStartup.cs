using Furion;
using Furion.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

[SuppressSniffer]
public abstract class DDCSStartup : AppStartup
{
    /// <summary>
    /// 配置Web服务启动
    /// </summary>
    /// <param name="services"></param>
    protected abstract void ConfigureServices(IServiceCollection services);

    /// <summary>
    /// 配置应用程序启动
    /// </summary>
    /// <param name="services"></param>
    protected abstract void ConfigureApplication(IServiceCollection services);

    /// <summary>
    /// 配置Web服务管道
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    protected abstract void Configure(IApplicationBuilder app, IWebHostEnvironment env);

    protected virtual ServiceRunTypeEnum? ServiceRunType { get; set; }

    public DDCSStartup()
    {
        ServiceRunType ??= App.GetConfig<ServiceRunTypeEnum>("ServiceRunType");
    }

    /// <summary>
    /// 该方法由框架调用，请勿重写或覆盖
    /// </summary>
    /// <param name="services"></param>
    public void ConfigureServicesBase(IServiceCollection services)
    {
        if (ServiceRunType == ServiceRunTypeEnum.WebApplication)
        {
            ConfigureServices(services);
        }
        else
        {
            ConfigureApplication(services);
        }
    }

    /// <summary>
    /// 该方法由框架调用，请勿重写或覆盖
    /// </summary>
    /// <param name="app"></param>
    /// <param name="env"></param>
    public void ConfigureBase(IApplicationBuilder app, IWebHostEnvironment env)
    {
        Configure(app, env);
    }
}
