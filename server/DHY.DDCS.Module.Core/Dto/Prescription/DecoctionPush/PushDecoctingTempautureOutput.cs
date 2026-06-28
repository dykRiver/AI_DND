/// <summary>
/// 推送煎煮温度输出
/// </summary>
public class PushDecoctingTempautureOutput
{
    /// <summary>
    /// 处方ID
    /// </summary>
    public long Pid { get; set; }
    /// <summary>
    /// 煎煮类型: 10 煎药开始 20 一煎开始 30一煎结束 40二煎开始 50 二煎结束 60 煎药完成 70 出药开始 80出药结束
    /// 610 先煎  611 先煎结束 620 另煎  621 另煎结束 630 后下 631 后下结束
    /// </summary>
    public DecoctStatusEnum TisaneType { get; set; }
    /// <summary>
    /// 煎煮时间
    /// </summary>
    public DateTime? TisaneTime { get; set; }
    /// <summary>
    /// 机器码
    /// </summary>
    public long MachineId { get; set; }
    /// <summary>
    /// 温度
    /// </summary>
    public decimal Temperature { get; set; }
}

