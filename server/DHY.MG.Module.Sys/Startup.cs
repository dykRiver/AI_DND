using DHY.MG.Module.Sys.Dtos;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace DHY.MG.Module.Sys
{
    /// <summary>
    /// Dispenssing模块启动项
    /// </summary>
    [AppStartup(200)]
    public class Startup : DDCSStartup
    {
        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddEventbusHandlers();
            services.AddConfigurableOptions<AliYunOptions>();
            services.AddConfigurableOptions<GradualClueOptions>();
            services.AddConfigurableOptions<DDBotOptions>();
        }
        protected override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {

        }
        protected override void ConfigureApplication(IServiceCollection services)
        {
            services.AddDevices();

        }
        [NativeApplicationEntry]
        public void RunApplication(
            IServiceProvider pd)
        {



        }
    }
}
