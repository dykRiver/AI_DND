using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using DHY.Game.Core.Options;
using DHY.Game.AI.Options;

namespace DHY.Game.Admin
{
    /// <summary>
    /// 游戏运营管理模块启动项
    /// </summary>
    [AppStartup(300)]
    public class Startup : DDCSStartup
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            // 注册游戏核心配置选项
            services.AddConfigurableOptions<GameOptions>();
            // 注册AI配置选项
            services.AddConfigurableOptions<GameAiOptions>();
        }

        protected override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        protected override void ConfigureApplication(IServiceCollection services)
        {
        }
    }
}
