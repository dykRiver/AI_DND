using Furion.Logging;
using Microsoft.Extensions.Logging;

namespace DHY.Core.Dto;

public class LogEventPayload
{
    /// <summary>
    /// 记录器类别名称
    /// </summary>
    public string LogName { get; set; }

    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel LogLevel { get; set; }

    /// <summary>
    /// 事件 Id
    /// </summary>
    public EventId EventId { get; set; }

    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 异常对象
    /// </summary>
    public Exception Exception { get; set; }

    /// <summary>
    /// 当前状态值
    /// </summary>
    /// <remarks>可以是任意类型</remarks>
    public object State { get; set; }

    /// <summary>
    /// 日志记录时间
    /// </summary>
    public DateTime LogDateTime { get; set; }

    /// <summary>
    /// 线程 Id
    /// </summary>
    public int ThreadId { get; set; }

    /// <summary>
    /// 是否使用 UTC 时间戳
    /// </summary>
    public bool UseUtcTimestamp { get; set; }

    /// <summary>
    /// 请求/跟踪 Id
    /// </summary>
    public string TraceId { get; set; }

    /// <summary>
    /// 日志上下文
    /// </summary>
    public LogContext Context { get; set; }
}
