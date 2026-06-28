using System.ComponentModel.DataAnnotations;

namespace DHY.DDCS.Module.Prescription.Dtos;

public class BindingContainerDDCSPrescriptionOutput
{
    /// <summary>
    /// 外部处方号
    /// </summary>
    [Required, MaxLength(100)]
    public string PrescriptionNo { get; set; }

    /// <summary>
    /// 患者姓名
    /// </summary>
    [Required, MaxLength(100)]
    public string PatientName { get; set; }

    /// <summary>
    /// 拆方加水量
    /// </summary>
    public int? WaterAmount { get; set; }

    /// <summary>
    /// 桶号
    /// </summary>
    public long? ContainerNo { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 处方桶类型： 1群药（常规）、2先煎、3后下，4另煎（单独包装）。另包不考虑、烊化不考虑
    /// </summary>
    public ContainerTypeEnum DecoctionType { get; set; }

    public string DecoctionTypeName => DecoctionType.GetDescription();
}
