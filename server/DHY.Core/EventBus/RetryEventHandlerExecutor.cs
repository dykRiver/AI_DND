using Furion.EventBus;
using Furion.FriendlyException;

namespace DHY.Core;

/// <summary>
/// 事件执行器-超时控制、失败重试熔断等等
/// </summary>
public class RetryEventHandlerExecutor : IEventHandlerExecutor
{
    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
    {
        // 如果执行失败，每隔 1s 重试，最多三次
        await Retry.InvokeAsync(async () =>
        {
            await handler(context);

        }, 3, 1000);
    }
}

public class InfiniteRetryEventHandlerExecutor : IEventHandlerExecutor
{
    public async Task ExecuteAsync(EventHandlerExecutingContext context, Func<EventHandlerExecutingContext, Task> handler)
    {
        // 如果执行失败，每隔 15s 重试，最多2147483647次
        await Retry.InvokeAsync(async () =>
        {
            await handler(context);

        }, int.MaxValue, 15000);
    }
}