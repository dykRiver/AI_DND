/// <summary>
/// 更新DDCS处方输入
/// </summary>
public class UpdateDDCSPrescriptionInput
{
    /// <summary>
    /// 拆方Id
    /// </summary>
    public long DDCSPid { get; set; }
    /// <summary>
    /// 目标得液量
    /// </summary>
    public int? TargetVolumn { get; set; }
    public int? RealVolumn { get; set; }
    /// <summary>
    /// 关联包装机
    /// 对应包装机的设备号
    /// </summary>
    public int? PackagingNo { get; set; }
}

