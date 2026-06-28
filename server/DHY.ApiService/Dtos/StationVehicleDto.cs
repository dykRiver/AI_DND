namespace DHY.InternalApiService.Dtos;

public class StationVehicleDto
{
    public long Id { get; set; }
    /// <summary>
    /// 车辆设备号；对应到设备表里设备号
    /// </summary>
    public int VehicleKey { get; set; }

    /// <summary>
    /// 车辆类型；注：一个车辆有多种类型，这里用int64表示，每1字节是一个类型（最多一个车辆属于8种类型）。第一个8位用于巷道号，其它未用。
    /// </summary>
    public long VehicleType { get; set; }

    /// <summary>
    /// 状态；1闲置，2忙碌（忙碌相当于锁定一辆车）
    /// </summary>
    public int Status { get; set; }
}
