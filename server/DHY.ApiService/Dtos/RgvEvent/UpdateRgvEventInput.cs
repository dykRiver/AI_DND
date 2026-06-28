using DHY.DDCS.Module.Core;

namespace DHY.InternalApiService;

public class UpdateRgvEventInput
{
    public long Id { get; set; }
    /// <summary>
    /// 设备号
    /// </summary>
    public ushort RgvDeviceNo { get; set; }

    /// <summary>
    /// 从设备号
    /// </summary>
    public ushort FromDeviceNo { get; set; }

    /// <summary>
    /// 到设备号
    /// </summary>
    public ushort ToDeviceNo { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    public RgvOperationTypeEnum OperationType { get; set; }

    /// <summary>
    /// 桶号
    /// </summary>
    public ushort ContainerNo { get; set; }

}
