using DHY.Core.Interfaces;

namespace DHY.Core.EventBus
{
    public sealed class MessageIdGenerationContext
    {
        public string MethodName { get; set; }



        public IRpcEventBusClientOptions Options { get; set; }
    }
}
