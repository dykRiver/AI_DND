using System.ComponentModel.DataAnnotations;

public class PrescriptionInfoAppDto
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
    /// 贴数/剂数
    /// </summary>
    [Required]
    public int Dosage { get; set; }

    /// <summary>
    /// 服用次数
    /// </summary>
    [Required]
    public int Frequency { get; set; }

    /// <summary>
    /// 服用方式
    /// </summary>
    [Required]
    public int Usage { get; set; }

    /// <summary>
    /// 服用方式
    /// </summary>
    public string TakeMethod { get; set; }
    /// <summary>
    /// 处方明细
    /// </summary>
    public List<PrescriptionInfoDetailOutput> Details { get; set; }

}