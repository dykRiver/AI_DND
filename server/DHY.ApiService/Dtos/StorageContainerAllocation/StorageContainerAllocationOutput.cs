namespace DHY.InternalApiService;

public class StorageContainerAllocationOutput
{
    /// <summary>
    /// 容器号
    /// </summary>
    public int ContainerNo { get; set; }
    /// <summary>
    /// 是否离开缓存区
    /// </summary>
    public int IsLeave { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}
