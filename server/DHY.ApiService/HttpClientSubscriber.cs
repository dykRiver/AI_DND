using DHY.Core.Consts;
using Furion.EventBus;

namespace DHY.InternalApiService;

internal class HttpClientSubscriber : IEventSubscriber
{
    private DateTime _lastRequestTime = DateTime.Now;
    [EventSubscribe(ComponentConst.HttpApiUnauthorizedEventName)]
    public async Task UnauthorizedEventHandle(EventHandlerExecutingContext context)
    {
        if (DateTime.Now.Subtract(_lastRequestTime) < TimeSpan.FromMinutes(5))
        {
            return;
        }

        _lastRequestTime = DateTime.Now;
        var loginApiService = App.GetRequiredService<ILoginApiService>();
        var httpClients = App.GetRequiredService<IOptions<HttpClientSettingOptions>>();
        await new Startup().RunApplicationAsync(loginApiService, httpClients);
    }
}
