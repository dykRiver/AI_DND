internal class MonitorLock 
{
    private readonly object _lockObject = new();
    public void Enter(TimeSpan timeOut = default)
    {
        if (timeOut == default)
        {
            timeOut = Timeout.InfiniteTimeSpan;
        }

        if (timeOut == Timeout.InfiniteTimeSpan)
        {
            // 无超时限制，直接使用 Monitor.Enter
            Monitor.Enter(_lockObject);
        }
        else
        {
            // 超时限制，使用 TryEnter 并捕获异常
            bool acquired = false;
            try
            {
                DateTime startTime = DateTime.UtcNow;
                TimeSpan remainingTime = timeOut;

                while (!acquired && remainingTime > TimeSpan.Zero)
                {
                    acquired = Monitor.TryEnter(_lockObject, remainingTime);
                    if (!acquired)
                    {
                        Thread.Sleep(1); // 避免忙等待
                        remainingTime = timeOut - (DateTime.UtcNow - startTime);
                    }
                }

                if (!acquired)
                {
                    throw new TimeoutException("Failed to acquire lock within the specified timeout.");
                }
            }
            catch (ThreadInterruptedException)
            {
                // 线程被中断时释放锁（如果已经获取）
                if (acquired)
                {
                    Monitor.Exit(_lockObject);
                }
                throw;
            }
        }
    }

    public void Exit()
    {
        if (Monitor.IsEntered(_lockObject))
        {
            Monitor.Exit(_lockObject);
        }
    }

    public bool GetLock()
    {
        throw new NotImplementedException();
    }

    public bool ReleaseLock()
    {
        throw new NotImplementedException();
    }
}