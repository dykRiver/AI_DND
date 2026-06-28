using Newtonsoft.Json;

/// <summary>
/// 拆方药品
/// </summary>
public class DDCSPrescriptionDetailPushOutput
{
    /// <summary>
    /// 拆分的序号。如拆成2个处方，序号分别为1，2。如果一个处方只是按先煎、群药、后下拆分，序号都是1。冗余。
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
    /// 调剂人/设备号
    /// </summary>
    [JsonProperty("AdjustNum")]
    public string AdjustNum { get; set; }
    /// <summary>
    /// 0=自动调剂，1=人工调剂
    /// </summary>
    [JsonProperty("IsAuto")]
    public bool IsAuto { get; set; }
    /// <summary>
    /// 调剂时间
    /// </summary>
    [JsonProperty("AdjustTime")]
    public DateTime AdjustTime { get; set; }
    /// <summary>
    /// 关联原始处方ID
    /// </summary>
    [JsonProperty("Pid")]
    public long Pid { get; set; }
    /// <summary>
    /// 控制系统的处方唯一Id (导航用)
    /// </summary>
    [JsonProperty("DDCSPid")]
    public long DDCSPid { get; set; }
    /// <summary>
    /// 药品Id，与煎药系统对应
    /// </summary>
    [JsonProperty("DrugId")]
    public long DrugId { get; set; }
    /// <summary>
    /// 本厂药品编码
    /// </summary>
    [JsonProperty("Code")]
    public string Code { get; set; }
    /// <summary>
    /// 本厂药品名称
    /// </summary>
    [JsonProperty("Name")]
    public string Name { get; set; }
    /// <summary>
    /// 药品规格
    /// </summary>
    [JsonProperty("Specification")]
    public string Specification { get; set; }
    /// <summary>
    /// 药品单位
    /// </summary>
    [JsonProperty("Unit")]
    public string Unit { get; set; }
    /// <summary>
    /// 单剂量
    /// </summary>
    [JsonProperty("SingleDosage")]
    public decimal SingleDosage { get; set; }
    /// <summary>
    /// 总剂量
    /// </summary>
    [JsonProperty("Weight")]
    public decimal Weight { get; set; }
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
}