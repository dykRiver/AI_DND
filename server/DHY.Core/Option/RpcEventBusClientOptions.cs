using DHY.Core.Interfaces;

namespace DHY.Core.EventBus
{
    public sealed class RpcEventBusClientOptions : IRpcEventBusClientOptions
    {
        public IRpcEventBusClientMessageIdGenerationStrategy TopicGenerationStrategy { get; set; } = new DefaultRpcEventBusClientMessageIdGenerationStrategy();
    }
}
