/// <summary>
/// 推送煎煮信息输出
/// </summary>
public class PushDecoctionInfoOutput
{
    /// <summary>
    /// 处方ID
    /// </summary>
    public long Pid { get; set; }
    /// <summary>
    /// 煎煮时间
    /// </summary>
    public DateTime? TisaneTime { get; set; }
    /// <summary>
    /// 机器ID
    /// </summary>
    public long MachineId { get; set; }
    /// <summary>
    /// 煎煮类型
    /// </summary>
    public DecoctStatusEnum TisaneType { get; set; }
}

