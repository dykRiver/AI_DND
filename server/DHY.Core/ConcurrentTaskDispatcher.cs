using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DHY.Core;

/// <summary>
/// 线程安全任务调度器
/// 支持多线程调用
/// </summary>
public abstract class ConcurrentTaskDispatcher<TTask> where TTask : class
{
    private int _dispatching;
    protected ILogger _logger;
    protected IServiceProvider _serviceProvider;
    protected ConcurrentQueue<TTask> _concurrentQueue;
    public ConcurrentTaskDispatcher(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetService<ILoggerFactory>().CreateLogger(GetType().Name);
        this._serviceProvider = serviceProvider;
        _concurrentQueue = new ConcurrentQueue<TTask>();
    }


    protected bool EnterDispatching()
    {
        return Interlocked.CompareExchange(ref _dispatching, 1, 0) == 0;
    }
    protected void ExitDispatching()
    {
        Interlocked.Exchange(ref _dispatching, 0);
    }

    /// <summary>
    /// 任务分配
    /// </summary>
    /// <remarks>
    /// 触发式分配，系统启动时会自动调用一次任务分配；
    /// 本方法线程安全；
    /// </remarks>
    /// <param name="parameter">分配参数，由具体的使用场景决定</param>
    public void TryDispatch(TTask taskData = null)
    {
        _concurrentQueue.Enqueue(taskData);
        DoDispatch();
    }

    protected virtual void DoDispatch()
    {
        if (EnterDispatching() == false)
        {
            return;
        }
        try
        {
            Dispatch();
        }
        catch
        {
            throw;
        }
        finally
        {
            ExitDispatching();
            if (_concurrentQueue.Count > 0)
            {
                DoDispatch();
            }
        }
    }

    //private void DoDispatch()
    //{
    //    if (EnterDispatching() == false)
    //    {
    //        return;
    //    }
    //    try
    //    {
    //        if(concurrentQueue.Count > 0)
    //        {
    //            //取出数据
    //            TTask task;
    //            concurrentQueue.TryDequeue(out task);

    //            //处理数据
    //            Dispatch(task);
    //        }

    //    }
    //    catch
    //    {
    //        throw;
    //    }
    //    finally
    //    {
    //        ExitDispatching();
    //        //if (concurrentQueue.Count > 0)
    //        //{
    //        //    DoDispatch();
    //        //}
    //    }
    //}

    /// <summary>
    /// 任务调度方法
    /// </summary>
    protected abstract void Dispatch();
}
