using Furion.EventBus;
namespace DHY.Core;

public static class RpcExtension
{
    public static TResult GetPayload<TResult>(this IEventSource source)
    {
        if (source.Payload == null)
        {
            return default;
        }

        return source.Payload.DeserializeJsonResult<TResult>();
    }
}