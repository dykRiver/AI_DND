using Furion.ConfigurableOptions;

namespace DHY.IO.MessageQueue.Amqp;

/// <summary>
/// 事件总线配置选项
/// </summary>
public sealed class EventBusOptions : IConfigurableOptions
{
    /// <summary>
    /// RabbitMQ
    /// </summary>
    public RabbitMQSettings RabbitMQ { get; set; }
}

/// <summary>
/// RabbitMQ
/// </summary>
public sealed class RabbitMQSettings
{
    /// <summary>
    /// 账号
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    /// 主机
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Mqtt端口
    /// </summary>
    public int MqttPort { get; set; }

    /// <summary>
    /// 通信分组名称
    /// </summary>
    public string CommunicationGroup { get; set; }

    /// <summary>
    /// 虚拟主机
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}