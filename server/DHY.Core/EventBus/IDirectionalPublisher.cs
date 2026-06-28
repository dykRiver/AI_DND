namespace DHY.Core.EventBus
{
    /// <summary>
    /// 定向发布器
    /// </summary>
    public interface IDirectionalPublisher
    {
        EventDirectional AcceptEventDirectional { get; }
        Task PublishAsync(DirectionalEventSource eventSource);
    }



}

