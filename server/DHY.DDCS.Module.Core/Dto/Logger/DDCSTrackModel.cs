public class DDCSTrackModel
{
    public long Id { get; set; }

    public virtual DateTime? CreateTime { get; set; }

    /// <summary>
    /// 事件类型
    /// </summary>
    public TrackEvent TrackEvent { get; set; }

    /// <summary>
    /// 原始处方Id
    /// </summary>
    public long Pid { get; set; }

    /// <summary>
    /// 拆方Id
    /// </summary>
    public long? DDCSPid { get; set; }

    /// <summary>
    /// 桶号
    /// </summary>
    public ushort? ContainerNo { get; set; }

    /// <summary>
    /// 设备/工位号
    /// </summary>
    public ushort? DeviceNo { get; set; }

    /// <summary>
    /// 对应的处方号
    /// </summary>
    public string PrescriptionNo { get; set; }

    /// <summary>
    /// 任务Id
    /// </summary>
    public long? TaskNo { get; set; }

    /// <summary>
    /// 当前操作人
    /// </summary>
    public string? OperUser { get; set; }

    /// <summary>
    /// （煎煮加水量，包装哪个患者的处方，调剂落药口的信息，桶在哪个位置，RGV的搬运过程，文火时间，武火时间等）
    /// </summary>
    public string? Extra1 { get; set; }

    public string? Extra2 { get; set; }

    public string? Extra3 { get; set; }

    public string? Extra4 { get; set; }

    public string? Extra5 { get; set; }

    public string? Extra6 { get; set; }

    public string? Extra7 { get; set; }

    public string? Extra8 { get; set; }

    public string? Extra9 { get; set; }
    public long? Elapsed { get; internal set; }
}