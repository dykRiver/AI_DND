using Microsoft.Extensions.Hosting;

namespace DHY.InternalApiService;

[AppStartup(99998)]
public class Startup : DDCSStartup
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        services.AddConfigurableOptions<HttpClientSettingOptions>();
    }

    protected override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        var lifeTime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

        lifeTime.ApplicationStarted.Register(() =>
        {
            // 因内部的API调用需要等待自身的webhost主机启动，所以这里异步延迟执行登录
            var loginApiService = app.ApplicationServices.GetRequiredService<ILoginApiService>();
            var httpClients = app.ApplicationServices.GetRequiredService<IOptions<HttpClientSettingOptions>>();

            RunApplicationAsync(loginApiService, httpClients).ConfigureAwait(false).GetAwaiter().GetResult();
        });
    }

    protected override void ConfigureApplication(IServiceCollection services)
    {
        services.AddConfigurableOptions<HttpClientSettingOptions>();
        services.AddEventBus(option =>
        {
            option.AddSubscriber<HttpClientSubscriber>();
        });
    }

    /// <summary>
    /// 应用程序入口点
    /// </summary>
    /// <param name="loginApiService"></param>
    /// <param name="httpClients"></param>
    /// <returns></returns>
    [NativeApplicationEntry]
    public async Task RunApplicationAsync(ILoginApiService loginApiService, IOptions<HttpClientSettingOptions> httpClients)
    {
        var clientAtt = loginApiService.GetType().BaseType.GetCustomAttribute<ClientAttribute>();
        var clientName = clientAtt == null ? "default" : clientAtt.Name;
        var loginClient = httpClients.Value.Clients.FirstOrDefault(s => s.ClientName.ToLower() == clientName.ToLower());

        if (loginClient == null)
        {
            return;
        }

        try
        {
            var loginResult = await loginApiService.LoginAsync(loginClient.Auth?.Data);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}