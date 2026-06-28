using System.ComponentModel.DataAnnotations;

public class DDCSPrescriptionDetailOutput : PrescriptionInfoDetailOutput
{
    /// <summary>
    /// 拆分的序号。如拆成2个处方，序号分别为1，2。如果一个处方只是按先煎、群药、后下拆分，序号都是1。冗余。
    /// </summary>
    [Required]
    public int Index { get; set; }

    /// <summary>
    /// 拆分后贴数
    /// </summary>
    [Required]
    public int Dosage { get; set; }

    /// <summary>
    /// 调剂人/设备号
    /// </summary>
    public string AdjustNum { get; set; }

    /// <summary>
    /// 0=自动调剂，1=人工调剂
    /// </summary>
    public bool IsAuto { get; set; }

    /// <summary>
    /// 调剂时间
    /// </summary>
    public DateTime? AdjustTime { get; set; }

    public object Clone()
    {
        return MemberwiseClone();
    }
}