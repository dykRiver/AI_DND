namespace DHY.DDCS.Module.Core.Abstraction;

/// <summary>
/// 基础数据模型
/// </summary>
public class DDCSModelBase
{
    /// <summary>
    /// 原始处方Id
    /// </summary>
    public long Pid { get; set; }

    /// <summary>
    /// 拆方号
    /// </summary>
    public long DDCSPid { get; set; }

    /// <summary>
    /// 处方号
    /// </summary>
    public string PrescriptionNo { get; set; }

    /// <summary>
    /// 容器号
    /// </summary>
    public int ContainerNo { get; set; }
    
    /// <summary>
    /// 任务号
    /// </summary>
    public long? TaskNo { get; set; }
}
