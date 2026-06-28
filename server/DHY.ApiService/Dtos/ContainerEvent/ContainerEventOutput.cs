using DHY.DDCS.Module.Core;

namespace DHY.InternalApiService;

public class ContainerEventOutput
{
    public long Id { get; set; }
    /// <summary>
    /// 容器号
    /// </summary>
    public int ContainerNo { get; set; }

    /// <summary>
    /// 设备号
    /// </summary>
    public ushort DeviceNo { get; set; }

    /// <summary>
    /// 桶事件类型
    /// </summary>
    public ContainerEventEnum ContainerEventType { get; set; }

    /// <summary>
    /// 处方id
    /// </summary>
    public long Pid { get; set; }

    /// <summary>
    /// 拆方ID
    /// </summary>
    public long DDCSPid { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime UpdateTime { get; set; }
}
