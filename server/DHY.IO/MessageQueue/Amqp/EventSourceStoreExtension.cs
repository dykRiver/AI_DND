using Furion;
using Furion.EventBus;
using RabbitMQ.Client;

namespace DHY.IO.MessageQueue.Amqp;

public static class EventSourceStoreExtension
{
    /// <summary>
    /// 添加Rabbitmq事件存储器
    /// </summary>
    /// <param name="eventBusOptions"></param>
    /// <returns></returns>
    public static EventBusOptionsBuilder AddRabbitMqEventSoureStore(this EventBusOptionsBuilder eventBusOptions)
    {
        // 创建默认内存通道事件源对象，可自定义队列路由key，如：dhy
        var eventBusOpt = App.GetConfig<EventBusOptions>("EventBus", true);
        if (eventBusOpt == null)
        {
            return eventBusOptions;
        }
        var rbmqEventSourceStorer = new RabbitMQEventSourceStore(new ConnectionFactory
        {
            UserName = eventBusOpt.RabbitMQ.UserName,
            Password = eventBusOpt.RabbitMQ.Password,
            HostName = eventBusOpt.RabbitMQ.HostName,
            VirtualHost = eventBusOpt.RabbitMQ.VirtualHost,
            Port = eventBusOpt.RabbitMQ.Port
        }, "dhy.eventbus", 3000, eventBusOpt.RabbitMQ.CommunicationGroup);

        // 替换默认事件总线存储器
        eventBusOptions.ReplaceStorer(serviceProvider =>
        {
            return rbmqEventSourceStorer;
        });
        return eventBusOptions;
    }
}
