public sealed class ServerCacheDto
{
    public string CacheKey { get; set; }
    public object CacheValue { get; set; }
    public TimeSpan? Expire { get; set; }
}
