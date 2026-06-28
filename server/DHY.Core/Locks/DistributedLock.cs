internal class DistributedLock 
{
    //通过缓存服务实现
    //private readonly ICache _cache;

    internal DistributedLock()
    {
        //TODO ：获取应用程序注入的缓存类
    }

    public void Enter(TimeSpan timeOut = default)
    {
        throw new NotImplementedException();
    }

    public void Exit()
    {
        throw new NotImplementedException();
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