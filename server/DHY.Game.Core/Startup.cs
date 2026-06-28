using DHY.Game.Core.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DHY.Game.Core
{
    /// <summary>
    /// 游戏核心模块启动项
    /// </summary>
    [AppStartup(300)]
    public class Startup : DDCSStartup
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddConfigurableOptions<GameOptions>();
        }

        protected override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }

        protected override void ConfigureApplication(IServiceCollection services)
        {
        }
    }
}
