using DHY.Game.AI.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DHY.Game.AI
{
    /// <summary>
    /// 游戏AI集成模块启动项
    /// </summary>
    [AppStartup(300)]
    public class Startup : DDCSStartup
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            // 注册AI配置
            services.AddConfigurableOptions<GameAiOptions>();

            // 注册HttpClient工厂
            services.AddHttpClient("DashScope", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            services.AddHttpClient("Poixe", client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
        }

        protected override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        protected override void ConfigureApplication(IServiceCollection services)
        {
        }
    }
}
