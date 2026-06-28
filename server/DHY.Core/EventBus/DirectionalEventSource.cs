using Furion.EventBus;
namespace DHY.Core.EventBus
{

    /// <summary>
    /// 定向方向
    /// </summary>
    public enum EventDirectional
    {
        None = 0,
        SignalR = 1,
        Websocket = 2,
        MessageQueue = 4,
        szls = 5,
    }

    /// <summary>
    /// 定向发送事件源
    /// 定向发送事件源是可以定向广播到指定分组的事件。
    /// 特性：
    /// 1、仍然遵循单点事件特性及功能；
    /// 2、可以指定定向分组，被指定定向分组的事件消息除了会发送到间点订阅者外，还广播到定向分组；
    /// </summary>
    public class DirectionalEventSource : IEventSource
    {
        public DirectionalEventSource()
        {
        }

        public DirectionalEventSource(string eventId, object payLoad)
        {
            EventId = eventId;
            Payload = payLoad;
        }

        public DirectionalEventSource(string eventId, object payLoad, EventDirectional eventDirectional)
        {
            EventId = eventId;
            Payload = payLoad;
            EventDirectional = eventDirectional;
        }

        /// <summary>
        /// 定向类型，默认值：EventDirectional.SignalR
        /// </summary>
        public EventDirectional EventDirectional { get; set; } = EventDirectional.SignalR;

        /// <summary>
        /// 定向分组
        /// 例如大屏使用websocket连接，发送到01号屏
        /// EventDirectional = EventDirectional.Websocket
        /// EventDirectionalGroup ="screen01"
        /// </summary>
        public string EventDirectionalGroup { get; set; }

        /// <summary>
        /// 事件 Id
        /// </summary>
        public string EventId { get; set; }

        /// <summary>
        /// 事件承载（携带）数据
        /// </summary>
        public object Payload { get; set; }

        /// <summary>
        /// 事件创建时间
        /// </summary>
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 消息是否只消费一次
        /// </summary>
        public bool IsConsumOnce { get; set; }  // Furion 4.9.1.24 添加

        /// <summary>
        /// 取消任务 Token
        /// </summary>
        /// <remarks>用于取消本次消息处理</remarks>
        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public CancellationToken CancellationToken { get; set; }
    }



}

