using Newtonsoft.Json;

/// <summary>
/// 拆方信息
/// </summary>
public class DDCSPrescriptionPushOutput
{
    /// <summary>
    /// 执行系统处方Id
    /// </summary>
    [JsonProperty("Id")]
    public long Id { get; set; }
    /// <summary>
    /// 处方id
    /// </summary>
    [JsonProperty("Pid")]
    public long Pid { get; set; }
    /// <summary>
    /// 拆分的序号
    /// </summary>
    [JsonProperty("SplitIndex")]
    public int Index { get; set; }
    /// <summary>
    /// 拆分后贴数
    /// </summary>
    [JsonProperty("SplitDosage")]
    public int Dosage { get; set; }
    /// <summary>
    /// 处方桶类型： 1群药（常规）、2先煎、3后下，4另煎（单独包装）。另包不考虑、烊化不考虑
    /// </summary>
    [JsonProperty("SplitPrescriptionType")]
    public ContainerTypeEnum DecoctionType { get; set; }
    /// <summary>
    /// 拆合方标志。0拆方，1合方（合方一般带着拆方，几个处方合在一起后，依然要按先煎、群药、后下等拆方）。
    /// </summary>
    [JsonProperty("SplitType")]
    public byte SplitType { get; set; }
    /// <summary>
    /// 创建时间
    /// </summary>
    [JsonProperty("CreateTime")]
    public DateTime CreateTime { get; set; }
    /// <summary>
    /// 修改时间
    /// </summary>  
    [JsonProperty("UpdateTime")]
    public DateTime UpdateTime { get; set; }
    /// <summary>
    /// 创建人Id
    /// </summary>
    [JsonProperty("CreateUserId")]
    public long CreateUserId { get; set; }
    /// <summary>
    /// 创建人姓名
    /// </summary>
    [JsonProperty("CreateUserName")]
    public string CreateUserName { get; set; }
    /// <summary>
    /// 修改人Id
    /// </summary>
    [JsonProperty("UpdateUserId")]
    public long UpdateUserId { get; set; }
    /// <summary>
    /// 修改人姓名
    /// </summary>
    [JsonProperty("UpdateUserName")]
    public string UpdateUserName { get; set; }
    /// <summary>
    /// 排序码
    /// </summary>
    [JsonProperty("OrderNo")]
    public int OrderNo { get; set; }
    /// <summary>
    /// 任务号
    /// </summary>
    public long TaskNo { get; set; }
    /// <summary>
    /// 拆方明细
    /// </summary>
    [JsonProperty("SplitedDrug")]
    public List<DDCSPrescriptionDetailPushOutput> Details { get; set; }
}