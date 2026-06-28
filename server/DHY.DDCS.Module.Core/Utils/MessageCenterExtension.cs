using DHY.Core.EventBus;
using Furion.EventBus;

namespace DHY.DDCS.Module.Core.Utils;

public static class MessageCenterExtension
{
    public static Task PublishDirectionalAsync(string eventId, object sender)
    {
        var processEventSource = new DirectionalEventSource(eventId, sender);
        processEventSource.EventDirectional = EventDirectional.None;

        return MessageCenter.PublishAsync(processEventSource);
    }
}
