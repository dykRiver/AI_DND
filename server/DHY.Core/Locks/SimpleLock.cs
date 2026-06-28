/// <summary>
/// 简单锁
/// </summary>
internal class SimpleLock
{
    private int _waiters = 0;
    private readonly AutoResetEvent _waiterLock = new(false);

    /// <summary>
    /// 获取锁
    /// </summary>
    /// <returns><c>true</c>锁可用，<c>false</c>锁不可用</returns>
    public bool GetLock()
    {
        return Interlocked.Increment(ref _waiters) == 1;
    }

    /// <summary>
    /// 释放锁
    /// </summary>
    /// <returns><c>true</c>释放成功<c>false</c>释放失败</returns>
    public bool ReleaseLock()
    {
        return Interlocked.Decrement(ref _waiters) == 0;
    }

    /// <summary>
    /// 持有锁，没有拿到就进入阻塞，直到其它持有线程释放
    /// </summary>
    public void Enter(TimeSpan timeOut = default)
    {
        if (timeOut == default)
        {
            timeOut = Timeout.InfiniteTimeSpan;
        }

        while (!GetLock())
        {
            if (!_waiterLock.WaitOne(timeOut))
            {
                throw new TimeoutException("Failed to acquire lock within the specified timeout.");
            }
        }
    }

    /// <summary>
    /// 释放锁，并唤醒一个正在等待获取锁的线程
    /// </summary>
    public void Exit()
    {
        if (ReleaseLock())
        {
            _waiterLock.Set();
        }
    }
}
