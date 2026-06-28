using DHY.Core;
using EasyNetQ;
using Furion.EventBus;
using Furion.JsonSerialization;
using Furion.Logging.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace DHY.IO.MessageQueue.Amqp;
public sealed class RabbitMQEventSourceStore : IEventSourceStorer, IDisposable
{
    /// <summary>
    /// 内存通道事件源存储器
    /// </summary>
    private readonly Channel<IEventSource> _channel;

    /// <summary>
    /// 通道对象
    /// </summary>
    private readonly IModel _model;

    /// <summary>
    /// 连接对象
    /// </summary>
    private readonly IConnection _connection;

    /// <summary>
    /// 路由键
    /// </summary>
    private readonly string _routeKey;

    /// <summary>
    /// 交换机名
    /// </summary>
    private readonly string _exchangeName;

    /// <summary>
    /// 队列
    /// </summary>
    private readonly string _queueName;

    #region 数字孪生
    /// <summary>
    /// 通道对象 数字孪生
    /// </summary>
    private readonly IModel _modelForSzls;

    /// <summary>
    /// 交换机名
    /// </summary>
    private readonly string _exchangeNameForSzls;

    /// <summary>
    /// 队列
    /// </summary>
    private readonly string _queueNameForSzls;

    #endregion

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="factory">连接工厂</param>
    /// <param name="routeKey">路由键</param>
    /// <param name="capacity">存储器最多能够处理多少消息，超过该容量进入等待写入</param>
    /// <param name="queueName">队列名称</param>
    /// <param name="communicationGroup">跨操作系统通讯分组，相同名称的分组都会收到总线信息</param>
    public RabbitMQEventSourceStore(ConnectionFactory factory, string routeKey, int capacity, string communicationGroup)
    {
        // 配置通道，设置超出默认容量后进入等待
        var boundedChannelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        var surrfix = string.Empty;

        if (!string.IsNullOrEmpty(communicationGroup))
        {
            surrfix = $".{communicationGroup}";
        }
        else
        {
            surrfix = $".{Environment.MachineName}";
        }

        // 创建有限容量通道
        _channel = Channel.CreateBounded<IEventSource>(boundedChannelOptions);

        //解决连接MQ，设置IPV4，根据部署的网络环境，IPV6会连接失败
        ConnectionFactory.DefaultAddressFamily = AddressFamily.InterNetwork;
        // 创建连接
        _connection = factory.CreateConnection();
        _routeKey = routeKey;
        _queueName = $"{Process.GetCurrentProcess().ProcessName}_{Process.GetCurrentProcess().Id}";
        _queueName += surrfix;

        // 创建通道
        _model = _connection.CreateModel();

        // 声明路由队列
        _model.QueueDeclare(_queueName, false, false, true, null);

        _exchangeName = $"DDCS.{ExchangeType.Fanout.ToUpper()}{surrfix}";

        //申明一个扇形交换机
        _model.ExchangeDeclare(_exchangeName, ExchangeType.Fanout, false, false);

        //_model.QueueBind(routeKey, defaultExchangeName, "");
        _model.QueueBind(_queueName, _exchangeName, routeKey);

        #region 数字孪生 通道

        _queueNameForSzls = $"szls_queue_power";
        // 创建通道
        _modelForSzls = _connection.CreateModel();

        // 声明路由队列
        _modelForSzls.QueueDeclare(_queueNameForSzls, false, false, true, null);
        //_modelForSzls.QueueDeclare(_queueNameForSzls + "A", false, false, true, null);

        _exchangeNameForSzls = $"DDCS.{ExchangeType.Fanout.ToUpper()}_szlsA";

        //申明一个扇形交换机
        _modelForSzls.ExchangeDeclare(_exchangeNameForSzls, ExchangeType.Fanout, false, false);

        _modelForSzls.QueueBind(_queueNameForSzls, _exchangeNameForSzls, routeKey);
        //_modelForSzls.QueueBind(_queueNameForSzls + "A", _exchangeNameForSzls, routeKey);
        #endregion

        // 创建消息订阅者
        var consumer = new EventingBasicConsumer(_model);
        var consumerForSzls = new EventingBasicConsumer(_modelForSzls);

        // 订阅消息并写入内存 Channel
        consumer.Received += (ch, ea) =>
        {
            // 读取原始消息
            var stringEventSource = Encoding.UTF8.GetString(ea.Body.ToArray());

            // 转换为 IEventSource，这里可以选择自己喜欢的序列化工具，如果自定义了 EventSource，注意属性是可读可写
            var eventSource = JSON.Deserialize<ChannelEventSource>(stringEventSource);
            // 写入内存管道存储器
            Task.Run(async () =>
            {
                await _channel.Writer.WriteAsync(eventSource);
                //$"总线写入消息{stringEventSource}".LogInformation();
            });

            // 确认该消息已被消费
            _model.BasicAck(ea.DeliveryTag, false);
        };

        // 订阅消息并写入内存 Channel 自接收数字孪生消息，验证数据
        //consumerForSzls.Received += (ch, ea) =>
        //{
        //    // 读取原始消息
        //    var stringEventSource = Encoding.UTF8.GetString(ea.Body.ToArray());

        //    // 转换为 IEventSource，这里可以选择自己喜欢的序列化工具，如果自定义了 EventSource，注意属性是可读可写
        //    var eventSource = JSON.Deserialize<ChannelEventSource>(stringEventSource);
        //    // 写入内存管道存储器
        //    Task.Run(async () =>
        //    {
        //        await _channel.Writer.WriteAsync(eventSource);
        //        //$"总线写入消息{stringEventSource}".LogInformation();
        //    });
        //    Log.Information($"数字孪生采集数据：{stringEventSource}");
        //    // 确认该消息已被消费
        //    _modelForSzls.BasicAck(ea.DeliveryTag, false);
        //};

        // 启动消费者 设置为手动应答消息
        _model.BasicConsume(_queueName, false, consumer);

        // 启动消费者 设置为手动应答消息
        //_modelForSzls.BasicConsume(_queueNameForSzls, false, consumerForSzls);
    }

    /// <summary>
    /// 将事件源写入存储器
    /// </summary>
    /// <param name="eventSource">事件源对象</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns><see cref="ValueTask"/></returns>
    public async ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken)
    {
        // 空检查
        if (eventSource == default)
        {
            throw new ArgumentNullException(nameof(eventSource));
        }

        // 这里判断是否是 ChannelEventSource 或者 自定义的 EventSource
        if (eventSource is ChannelEventSource source)
        {
            var channelSourceJson = JSON.Serialize(source);
            // 序列化，这里可以选择自己喜欢的序列化工具
            var data = Encoding.UTF8.GetBytes(channelSourceJson);

            if (data.Length >= 16777216)
            {
                $"写入数据超出最大限制:{channelSourceJson.Substring(0, 500)}...".LogError();
                return;
            }

            if (eventSource.EventId.IndexOf("productkey") > -1)
            {
                _modelForSzls.BasicPublish(_exchangeNameForSzls, _routeKey, null, data);
            }
            else
            {
                _model.BasicPublish(_exchangeName, _routeKey, null, data);
            }

        }
        else
        {
            // 这里处理动态订阅问题
            await _channel.Writer.WriteAsync(eventSource, cancellationToken);
        }
    }

    /// <summary>
    /// 从存储器中读取一条事件源
    /// </summary>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns>事件源对象</returns>
    public async ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken)
    {
        // 读取一条事件源
        var eventSource = await _channel.Reader.ReadAsync(cancellationToken);
        //$"总线收到消息:{eventSource.ToJson()}".LogInformation();
        return eventSource;
    }

    /// <summary>
    /// 释放非托管资源
    /// </summary>
    public void Dispose()
    {
        _model.Dispose();
        _connection.Dispose();
    }
}
/// <summary>
/// RabbitMQ自定义事件源存储器
/// </summary>
public class RabbitMQEventSourceStore2 : IEventSourceStorer
{
    /// <summary>
    /// 内存通道事件源存储器
    /// </summary>
    private readonly Channel<IEventSource> _channel;

    /// <summary>
    /// 通道对象
    /// </summary>
    //private readonly IModel _model;

    //private readonly IModel _consumerModel;
    /// <summary>
    /// 连接对象
    /// </summary>
    //private readonly IConnection _connection;

    /// <summary>
    /// 路由键
    /// </summary>
    private readonly string _routeKey;
    private string exchangeName = "rpceventbus";



    private IBus _bus;
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="factory">连接工厂</param>
    /// <param name="routeKey">路由键</param>
    /// <param name="capacity">存储器最多能够处理多少消息，超过该容量进入等待写入</param>
    public RabbitMQEventSourceStore2(ConnectionFactory factory, string routeKey, int capacity)
    {
        // 配置通道，设置超出默认容量后进入等待
        var boundedChannelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        var connectionStr = $"host={factory.HostName};virtualHost={factory.VirtualHost};username={factory.UserName};password={factory.Password}";
        ; _bus = RabbitHutch.CreateBus(connectionStr);


        // 创建有限容量通道
        _channel = Channel.CreateBounded<IEventSource>(boundedChannelOptions);

        // 创建连接
        //_connection = factory.CreateConnection();
        _routeKey = routeKey;

        // 创建通道
        //_model = _connection.CreateModel();
        //_consumerModel = _connection.CreateModel();

        //申明一个扇形交换机
        //_model.ExchangeDeclare(exchangeName,  "fanout",false,false);
        //_consumerModel.ExchangeDeclare(exchangeName, "fanout", false, false);
        //接收用：声明路由队列
        //_consumerModel.QueueDeclare(routeKey, false, false, false, null);

        //consumerModel.QueueBind(routeKey, exchangeName, "");
        // 创建消息订阅者
        _bus.PubSub.Subscribe<ChannelEventSource>(_routeKey, async msg =>
        {
            // 读取原始消息
            //var stringEventSource = Encoding.UTF8.GetString(msg.ToArray());

            // 转换为 IEventSource，如果自定义了 EventSource，注意属性是可读可写
            //var eventSource = JSON.Deserialize<ChannelEventSource>(stringEventSource);

            // 写入内存管道存储器
            await _channel.Writer.WriteAsync(msg);
            $"MQEventSourceStore订阅端收到消息并写入消息总线完成:{msg}".LogInformation();

            // 确认该消息已被消费
            //  _consumerModel.BasicAck(ea.DeliveryTag, false);

        });

        //var consumer = new EventingBasicConsumer(_consumerModel);

        //// 订阅消息并写入内存 Channel
        //consumer.Received += (ch, ea) =>
        //{
        //    Console.WriteLine($"model recive message:{ea}");
        //    // 读取原始消息
        //    var stringEventSource = Encoding.UTF8.GetString(ea.Body.ToArray());

        //    // 转换为 IEventSource，如果自定义了 EventSource，注意属性是可读可写
        //    var eventSource = JSON.Deserialize<ChannelEventSource>(stringEventSource);

        //    // 写入内存管道存储器
        //    _channel.Writer.WriteAsync(eventSource);

        //    // 确认该消息已被消费
        //    _consumerModel.BasicAck(ea.DeliveryTag, false);
        //};

        //// 启动消费者且设置为手动应答消息
        //_consumerModel.BasicConsume(routeKey, false, consumer);
    }

    /// <summary>
    /// 将事件源写入存储器
    /// </summary>
    /// <param name="eventSource">事件源对象</param>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns><see cref="ValueTask"/></returns>
    public async ValueTask WriteAsync(IEventSource eventSource, CancellationToken cancellationToken)
    {
        if (eventSource == default)
            throw new ArgumentNullException(nameof(eventSource));

        // 判断是否是 ChannelEventSource 或自定义的 EventSource
        if (eventSource is ChannelEventSource source)
        {
            var jsonData = JSON.Serialize(source);
            // 序列化及发布
            var data = Encoding.UTF8.GetBytes(jsonData);
            await _bus.PubSub.PublishAsync<ChannelEventSource>(source).ConfigureAwait(false);
            $"MQEventSourceStore发送消息到总线完成 {exchangeName}:{jsonData}".LogInformation();

        }
        else
        {
            // 处理动态订阅
            await _channel.Writer.WriteAsync(eventSource, cancellationToken);
        }
    }

    /// <summary>
    /// 从存储器中读取一条事件源
    /// </summary>
    /// <param name="cancellationToken">取消任务 Token</param>
    /// <returns>事件源对象</returns>
    public async ValueTask<IEventSource> ReadAsync(CancellationToken cancellationToken)
    {
        var eventSource = await _channel.Reader.ReadAsync(cancellationToken);
        //if (eventSource.EventId.IsNullOrEmpty() == false)
        {
            $"MQEventSourceStore从存储器中读到消息:{eventSource.ToJson()}".LogInformation();
        }
        return eventSource;
    }

    /// <summary>
    /// 释放非托管资源
    /// </summary>
    public void Dispose()
    {
        _bus.Dispose();
        // _connection.Dispose();
    }
}