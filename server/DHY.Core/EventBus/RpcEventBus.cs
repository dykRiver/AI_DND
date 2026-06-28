using System.Collections.Concurrent;
using DHY.Core.Interfaces;
using Furion;
using Furion.EventBus;
using Furion.Extensitions.EventBus;
using Furion.FriendlyException;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace DHY.Core.EventBus
{
    public static class EventBusSetup
    {

        /// <summary>
        /// 添加RPC事件服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddRpcEventBus(this IServiceCollection services)
        {

            services.AddTransient<IRpcEventBusClientOptions, RpcEventBusClientOptions>();
            services.AddSingleton<RpcEventBus>();
            return services;
        }
    }

    /// <summary>
    /// 远程过程调用
    /// 需要应答方处理正则通配消息ID
    /// </summary>
    public class RpcEventBus
    {
        private readonly IRpcEventBusClientOptions _options;

        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _waitingCalls = new ConcurrentDictionary<string, TaskCompletionSource<object>>();

        private IEventBusFactory _eventBusFactory;

        private ILogger _logger;
        public RpcEventBus(IEventBusFactory eventBusFactory, IRpcEventBusClientOptions options, ILogger<RpcEventBus> logger)
        {
            _eventBusFactory = eventBusFactory;
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }



        public void Dispose()
        {
            foreach (var tcs in _waitingCalls)
            {
                tcs.Value.TrySetCanceled();
            }

            _waitingCalls.Clear();
        }

        /// <summary>
        /// 执行一个RPC请求
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="timeout">超时时间</param>
        /// <param name="methodName">方法名</param>
        /// <param name="payload">消息内容</param>
        /// <returns>应答包</returns>
        /// <exception cref="TimeoutException"></exception>
        public async Task<TResult> ExecuteAsync<TResult>(TimeSpan timeout, string methodName, object payload)              
        {

            using (var timeoutToken = new CancellationTokenSource(timeout))
            {
                try
                {
                    return await ExecuteAsync<TResult>(methodName, payload, timeoutToken.Token);
                }
                catch (OperationCanceledException exception)
                {
                    if (timeoutToken.IsCancellationRequested)
                    {
                        _logger.LogError(exception.Message);
                        throw new TimeoutException($"执行{methodName}超时", exception);

                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// 注册RPC事件处理
        /// </summary>
        /// <param name="methodName">要注册的RPC事件名称，全局唯一</param>
        /// <param name="rpcEventHandler">事件处理器，返回事件结果</param>
        public void AddRpcHandler<TResult>(string methodName, Func<EventHandlerExecutingContext, Task<TResult>> rpcEventHandler)
        {
            var option = App.GetRequiredService<IRpcEventBusClientOptions>();
            if (option == null)
            {
                throw Oops.Oh("缺少IRpcEventBusClientOptions组件，请先在初始化时调用AddRpcEventBus()方法");
            }
            var context = new MessageIdGenerationContext() { Options = option, MethodName = methodName };
            var topicNames = option.TopicGenerationStrategy.CreateRpcTopics(context);

            MessageCenter.Subscribe(topicNames.RegisterMessageId, async ctx =>
            {
                var responseMessageId = $"{ctx.Source.EventId}:{option.TopicGenerationStrategy.ResponseSuffix}";


                var callResult = await rpcEventHandler(ctx);
                await MessageCenter.PublishAsync(responseMessageId, callResult);
                //_logger.LogInformation($"总线事件处理完成，发布回复:eventid={responseMessageId},{callResult.ToJson()},{ctx.ToJson()}");
            }, new EventSubscribeAttribute(topicNames.RegisterMessageId) { FuzzyMatch = true });
        }

        public void AddRpcHandler<TResult>(string methodName, Func<EventHandlerExecutingContext, TResult> rpcEventHandler)
        {
            var option = App.GetRequiredService<IRpcEventBusClientOptions>();
            if (option == null)
            {
                throw Oops.Oh("缺少IRpcEventBusClientOptions组件，请先在初始化时调用AddRpcEventBus()方法");
            }
            var context = new MessageIdGenerationContext() { Options = option, MethodName = methodName };
            var topicNames = option.TopicGenerationStrategy.CreateRpcTopics(context);

            MessageCenter.Subscribe(topicNames.RegisterMessageId, async ctx =>
            {
                var responseMessageId = $"{ctx.Source.EventId}:{option.TopicGenerationStrategy.ResponseSuffix}";


                var callResult =  rpcEventHandler(ctx);
                await MessageCenter.PublishAsync(responseMessageId, callResult);
                //_logger.LogInformation($"总线事件处理完成，发布回复:eventid={responseMessageId},{callResult.ToJson()}");
            }, new EventSubscribeAttribute(topicNames.RegisterMessageId) { FuzzyMatch = true });
        }

        public void AddRpcHandler(Enum methodName, Func<EventHandlerExecutingContext, Task<object>> rpcEventHandler)
        {
            AddRpcHandler(methodName.ParseToString(), rpcEventHandler);
        }


        /// <summary>
        /// 远程调用
        /// </summary>
        /// <typeparam name="TResult">远程调用返回结果</typeparam>
        /// <param name="methodName">调用方法名</param>
        /// <param name="payload">远程调用入参</param>
        /// <param name="cancellationToken">取消执行token</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<TResult> ExecuteAsync<TResult>(string methodName, object payload, CancellationToken cancellationToken = default)  
        {
            ArgumentNullException.ThrowIfNull(methodName);

            var context = new MessageIdGenerationContext() { Options = _options, MethodName = methodName };
            var topicNames = _options.TopicGenerationStrategy.CreateRpcTopics(context);

            var requestTopic = topicNames.RequestMessageId;
            var responseTopic = topicNames.ResponseMessageId;

            if (string.IsNullOrWhiteSpace(requestTopic))
            {
                throw new ArgumentNullException("RPC request topic is empty.");
            }

            if (string.IsNullOrWhiteSpace(responseTopic))
            {
                throw new ArgumentNullException("RPC response topic is empty.");
            }

            try
            {
                var awaitable = new TaskCompletionSource<object>();

                if (!_waitingCalls.TryAdd(responseTopic, awaitable))
                {
                    throw new InvalidOperationException();
                }


                var subRet = _eventBusFactory.Subscribe(responseTopic, HandleEventMessageReceivedAsync<TResult>).Wait(10 * 1000);
                //  _logger.LogInformation($"总线RPC订阅:{responseTopic}:{subRet}");

                await MessageCenter.PublishAsync(requestTopic, payload).ConfigureAwait(false);
                //    _logger.LogInformation($"发布总线消息eventid={requestTopic}完成");
                using (cancellationToken.Register(
                           () =>
                           {
                               //   _logger.LogInformation("总线应答等待被取消 ");
                               awaitable.TrySetCanceled();
                           }))
                {
                    return (TResult)await awaitable.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                _ = _waitingCalls.TryRemove(responseTopic, out _);
                await _eventBusFactory.Unsubscribe(responseTopic).ConfigureAwait(false);
                //  _logger.LogInformation($"取消订阅RPC总线消息({responseTopic})完成");

            }
        }

        /// <summary>
        /// 应答消息处理
        /// </summary>
        /// <typeparam name="TResult">应答数据返回结果</typeparam>
        /// <param name="eventArgs">应答入参</param>
        /// <returns></returns>
        private Task HandleEventMessageReceivedAsync<TResult>(EventHandlerExecutingContext eventArgs)
        {
            var data = eventArgs.Source.GetPayload<TResult>();
            var responseId = eventArgs.Source.EventId;

            if (!_waitingCalls.TryRemove(responseId, out var awaitable))
            {
                return Task.CompletedTask;
            }
            //  _logger.LogInformation($"收到RPC应答{responseId},HandleEventMessageReceivedAsync：设置返回");
            awaitable.TrySetResult(data);
            return Task.CompletedTask;
        }
    }



}

