using DHY.DDCS.Module.Prescription.Option;
using DHY.DDCS.Module.Prescription.PrescriptionSplit;
using Furion;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace DHY.DDCS.Module.Dispensing
{
    /// <summary>
    /// 处方模块启动项
    /// </summary>
    [AppStartup(300)]
    public class Startup : AppStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddConfigurableOptions<PrescriptionOptions>();

            var providers = App.EffectiveTypes.Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(AbstractSplitProvider)));
            var splitOptions = App.GetOptions<PrescriptionOptions>();

        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
        }
    }
}
