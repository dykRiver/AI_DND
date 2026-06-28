using DHY.Game.Hub.Options;
using DHY.Game.Hub.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DHY.Game.Hub
{
    /// <summary>
    /// 游戏实时通信模块启动项
    /// </summary>
    [AppStartup(300)]
    public class Startup : DDCSStartup
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddConfigurableOptions<GameSignalROptions>();

            // 注册GameSessionManager为Singleton(全局管理连接映射)
            services.AddSingleton<GameSessionManager>();
        }

        protected override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        protected override void ConfigureApplication(IServiceCollection services)
        {
        }
    }
}
