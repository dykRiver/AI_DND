/// <summary>
/// 处方推送至调剂系统适配输出类
/// </summary>
public class PrescriptionPushOutput
{
    /// <summary>
    /// 原始处方信息
    /// </summary>
    public PrescriptionOutput Prescription { get; set; } = new PrescriptionOutput();
    /// <summary>
    /// 拆方信息
    /// </summary>
    public List<DDCSPrescriptionPushOutput> TaskList { get; set; }
}

/// <summary>
/// 处方推送至管理系统适配输出类
/// </summary>
public class PrescriptionManagementSystemPushOutput
{
    /// <summary>
    /// 拆方信息
    /// </summary>
    public List<DDCSPrescriptionPushOutput> TaskList { get; set; }
}