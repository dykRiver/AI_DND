namespace DHY.Core.Interfaces
{

    public interface IRpcEventBusClientOptions
    {
        IRpcEventBusClientMessageIdGenerationStrategy TopicGenerationStrategy { get; set; }
    }
}
