using DHY.Core.EventBus;

namespace DHY.Core;

/// <summary>
/// 默认事件执行监视器
/// </summary>
public class DefaultEventHandlerMonitor : IEventHandlerMonitor
{
    private readonly ILogger<DefaultEventHandlerMonitor> _logger;
    private readonly List<IDirectionalPublisher> _publishers;
    public DefaultEventHandlerMonitor(ILogger<DefaultEventHandlerMonitor> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _publishers = serviceProvider.GetServices<IDirectionalPublisher>().ToList();
    }

    public Task OnExecutingAsync(EventHandlerExecutingContext context)
    {
        _logger.LogInformation("执行之前：{EventId}", context.Source.EventId);
        return Task.CompletedTask;
    }

    public Task OnExecutedAsync(EventHandlerExecutedContext context)
    {
        _logger.LogInformation("执行之后：{EventId}", context.Source.EventId);
        if (context.Source is DirectionalEventSource)
        {
            PubDirectionalEvent(context.Source as DirectionalEventSource);
        }
        if (context.Exception != null)
        {
            _logger.LogError(context.Exception, "执行出错啦：{EventId}", context.Source.EventId);
        }

        return Task.CompletedTask;
    }
    /// <summary>
    /// 发布定向事件
    /// </summary>
    /// <param name="eventSource"></param>
    private void PubDirectionalEvent(DirectionalEventSource eventSource)
    {
        var publisher = _publishers.FirstOrDefault(s => s.AcceptEventDirectional == eventSource.EventDirectional);
        publisher?.PublishAsync(eventSource);
    }
}
