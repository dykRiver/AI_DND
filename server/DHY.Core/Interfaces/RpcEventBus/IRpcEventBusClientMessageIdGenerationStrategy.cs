using DHY.Core.EventBus;

namespace DHY.Core.Interfaces
{
    public interface IRpcEventBusClientMessageIdGenerationStrategy
    {
        public string ResponseSuffix { get; }
        public string PreSuffix { get; }

        RpcEventBusMessageIdPair CreateRpcTopics(MessageIdGenerationContext context);
    }
}
