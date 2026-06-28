using DHY.DDCS.Module.Core;

namespace DHY.InternalApiService;

public class WaterUsageRecordOutput
{
    /// <summary>
    /// 外部处方号
    /// </summary>
    public string? PrescriptionNo { get; set; }

    /// <summary>
    /// 处方Id
    /// </summary>
    public long Pid { get; set; }

    /// <summary>
    /// 拆方Id
    /// </summary>
    public long DDCSPid { get; set; }

    /// <summary>
    /// 流程区域
    /// </summary>
    public ProcessAreaEnum ProcessArea { get; set; }

    /// <summary>
    /// 处方状态
    /// </summary>
    public PrescriptionManageStatusEnum State { get; set; }

    /// <summary>
    /// 用水量
    /// </summary>
    public int? WaterUsage { get; set; }

    /// <summary>
    /// 用水类型
    /// </summary>
    public AddWaterProcessEnum WaterType { get; set; }

    /// <summary>
    /// 设备号
    /// </summary>
    public ushort DeviceNo { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public StationTypeEnum DeviceType { get; set; }
}
