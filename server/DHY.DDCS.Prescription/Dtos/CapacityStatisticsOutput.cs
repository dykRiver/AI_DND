namespace DHY.DDCS.Module.Prescription.Dtos;

/// <summary>
/// 统计产能信息输出
/// </summary>
public class CapacityStatisticsOutput
{
    /// <summary>
    /// 折线图数据标识 示例 ['接收处方', '调剂完成','复核完成','浸泡完成','煎煮完成','包装完成']
    /// </summary>
    public string[] LegendData { get; set; }
    /// <summary>
    /// 折线图数据项：示例 ['6:00', '7:00', '8:00', '9:00', '10:00', '11:00', '12:00', '13:00', '14:00', '15:00', '16:00', '17:00', '18:00', '19:00', '20:00', '21:00', '22:00', '23:00']
    /// </summary>
    public string[] XAxis { get; set; }
    /// <summary>
    /// 折线图数据：示例 [0, 0, 32,0,12,0,3,4,5,6,20,8,9,1,23,24,1,0],  [0, 0, 32,0,12,0,3,4,5,6,20,8,9,]等几个状态的数据信息
    /// </summary>
    public List<int[]> SeriesData { get; set; } = new List<int[]>();
}
