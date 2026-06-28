using DHY.DDCS.Module.Common.Entity;

namespace DHY.DDCS.Module.Prescription.Dtos;
/// <summary>
/// 拆方前端VM
/// </summary>
public class DDCSPrescriptionOutput
{
    /// <summary>
    /// 拆分处方id
    /// </summary>
    public long Id { get; set; }
    /// <summary>
    /// 处方id，与煎药系统对应。对于合方，为其中的一个。
    /// </summary>
    public long Pid { get; set; }

    /// <summary>
    /// 拆分的序号。如拆成2个处方，需要分别为1，2
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// 拆分后贴数
    /// </summary>
    public int Dosage { get; set; }

    /// <summary>
    /// 处方桶类型： 1群药（常规）、2先煎、3后下，4另煎（单独包装）。另包不考虑、烊化不考虑
    /// </summary>
    public ContainerTypeEnum DecoctionType { get; set; }

    /// <summary>
    /// 拆合方标志。0拆方，1合方（合方一般带着拆方，几个处方合在一起后，依然要按先煎、群药、后下等拆方）。
    /// </summary>
    public byte SplitType { get; set; }

    /// <summary>
    /// 处方明细
    /// </summary>
    public List<DDCSPrescriptionDetail> Details { get; set; }

    /// <summary>
    /// 组合标签（树）
    /// </summary>
    public string Label => $"{Index}-{Pid}-{DecoctionType.GetDescription() ?? "未知"}";

}