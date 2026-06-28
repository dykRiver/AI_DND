using DHY.Core.EventBus;
using Microsoft.AspNetCore.SignalR;

namespace DHY.FrameWork.Core.SignalR
{
    /// <summary>
    /// 将事件消息转发到SignalR
    /// </summary>
    public class SignalREventPublisher : IDirectionalPublisher
    {
        private readonly IHubContext<OnlineUserHub, IOnlineUserHub> _onlineUserHubContext;

        public SignalREventPublisher(IHubContext<OnlineUserHub, IOnlineUserHub> onlineUserHubContext)
        {
            _onlineUserHubContext = onlineUserHubContext;
        }

        public EventDirectional AcceptEventDirectional => EventDirectional.SignalR;


        /// <summary>
        /// 发布定向事件消息到SingalR
        /// </summary>
        /// <param name="eventSource"></param>
        /// <returns></returns>
        public async Task PublishAsync(DirectionalEventSource eventSource)
        {
            await _onlineUserHubContext.Clients.All.PublishDirectionalMessage(eventSource);

        }
    }
}
