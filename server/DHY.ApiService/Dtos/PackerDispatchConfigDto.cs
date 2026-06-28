namespace DHY.InternalApiService;

public sealed class PackerDispatchConfigDto
{
    /// <summary>
    /// 包装机号
    /// </summary>
    public ushort PackerDeviceNo { get; set; }

    /// <summary>
    /// 医院Id
    /// </summary>
    public long HospitalId { get; set; }

    /// <summary>
    /// 巷道号
    /// </summary>
    public ushort RoadWayNo { get; set; }

    /// <summary>
    /// 包装卷Id
    /// </summary>
    public long PackingMaterialId { get; set; }
}
