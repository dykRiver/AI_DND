using DHY.Core.Interfaces;

namespace DHY.Core.EventBus
{
    public sealed class DefaultRpcEventBusClientMessageIdGenerationStrategy : IRpcEventBusClientMessageIdGenerationStrategy
    {
        public string ResponseSuffix { get => "response"; }

        public string PreSuffix => "rpceventbus";



        public RpcEventBusMessageIdPair CreateRpcTopics(MessageIdGenerationContext context)
        {


            var requestTopic = $"{PreSuffix}:{Guid.NewGuid()}:{context.MethodName}";
            var responseTopic = requestTopic + $":{ResponseSuffix}";

            return new RpcEventBusMessageIdPair
            {
                RequestMessageId = requestTopic,
                ResponseMessageId = responseTopic,
                RegisterMessageId = "^" + PreSuffix + ":[0-9a-fA-F]{8}(-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}:" + context.MethodName + "$"
            };
        }
    }
}
