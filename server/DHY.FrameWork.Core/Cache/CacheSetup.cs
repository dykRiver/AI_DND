namespace DHY.Core;

public static class CacheSetup
{
    /// <summary>
    /// 缓存注册（新生命Redis组件）
    /// </summary>
    /// <param name="services"></param>
    public static void AddCache(this IServiceCollection services)
    {
        ICache cache = Cache.Default;

        var cacheOptions = App.GetConfig<CacheOptions>("Cache", true);
        if (cacheOptions.CacheType == CacheTypeEnum.Redis.ToString())
        {
            cache = new FullRedis(new RedisOptions
            {
                Configuration = cacheOptions.Redis.Configuration,
                Prefix = cacheOptions.Redis.Prefix,
                Timeout = cacheOptions.Redis.Timeout,
            });
            if (cacheOptions.Redis.MaxMessageSize > 0)
                ((FullRedis)cache).MaxMessageSize = cacheOptions.Redis.MaxMessageSize;
        }

        services.AddSingleton(cache);
    }
}