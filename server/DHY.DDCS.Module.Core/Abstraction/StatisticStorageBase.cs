using Furion.DependencyInjection;

namespace DHY.DDCS.Module.Core;
[SuppressSniffer]
public abstract class StatisticStorageBase
{
    /// <summary>
    /// 设备号
    /// </summary>
    public ushort DeviceNo { get; set; }

    /// <summary>
    /// 容器号
    /// </summary>
    public ushort? ContainerNo { get; set; }

    /// <summary>
    /// 处方Id
    /// </summary>
    public long? Pid { get; set; }

    /// <summary>
    /// 拆方号
    /// </summary>
    public long? DDCSPid { get; set; }

    /// <summary>
    /// 工作状态（1是2否）
    /// </summary>
    public byte? WorkStatus { get; set; }

    /// <summary>
    /// 处方号
    /// </summary>
    public string PrescriptionNo { get; set; }

    /// <summary>
    /// 设备/工位类型
    /// </summary>
    public StationTypeEnum StationType { get; set; }

    /// <summary>
    /// 处方状态
    /// </summary>
    public PrescriptionManageStatusEnum State { get; set; }

    /// <summary>
    /// 所属区域
    /// </summary>
    public ProcessAreaEnum ProcessArea { get; set; }
}
