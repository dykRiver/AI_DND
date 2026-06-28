namespace DHY.DDCS.Module.Prescription.Dtos;

/// <summary>
/// 传给东程的原始处方信息；同时也用于执行系统限制不满足条件的处方不能推送
/// </summary>
public class PrePushInputBakModel
{
    public DDCSForDongChengPrescription Prescription { get; set; }
}
public class DDCSForDongChengPrescription
{
    /// <summary>
    /// 煎药系统主键
    /// </summary>
    public string Pid { get; set; }
    /// <summary>
    /// 医院编号
    /// </summary>
    public string Hospital_Num { get; set; }
    /// <summary>
    /// 医院名称
    /// </summary>
    public string Hospital_Name { get; set; }
    /// <summary>
    /// 处方号
    /// </summary>
    public string Pspnum { get; set; }
    /// <summary>
    /// 给后续泡药、煎药流程使用的条码，会打印在我们的调配单上
    /// </summary>
    public string Pcode { get; set; }
    /// <summary>
    /// 患者姓名
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// 性别：1：男，2：女 ，9： 不详
    /// </summary>
    public int Sex { get; set; }
    /// <summary>
    /// 年龄
    /// </summary>
    public string Age { get; set; }
    /// <summary>
    /// 电话
    /// </summary>
    public string Phone { get; set; }
    /// <summary>
    /// 地址
    /// </summary>
    public string Address { get; set; }

    /// <summary>
    /// 贴数
    /// </summary>
    public int Dose { get; set; }
    /// <summary>
    /// 次数
    /// </summary>
    public int TakeNum { get; set; }
    /// <summary>
    /// 煎药方式
    /// </summary>
    public string TypeName { get; set; }
    /// <summary>
    /// 药物数量
    /// </summary>
    public int DrugCount { get; set; }

    /// <summary>
    /// 包装量
    /// </summary>
    public int PackageNum { get; set; }

    /// <summary>
    /// 处方类型
    /// </summary>
    public string TypeTitleName { get; set; }
    /// <summary>
    /// 备注
    /// </summary>
    public string Remark { get; set; }
    /// <summary>
    /// 是否代配 0:不是代配 1:代配
    /// </summary>
    public int IsDaiPei { get; set; }
    /// <summary>
    /// 是否加急；0:否，1:是
    /// </summary>
    public int isurgent { get; set; }

    /// <summary>
    /// 2住院，1门诊
    /// </summary>
    public string HosOrOutPattient { get; set; }
    /// <summary>
    /// 科室
    /// </summary>
    public string Department { get; set; }
    /// <summary>
    /// 病区
    /// </summary>
    public string InpatientArea { get; set; }
    /// <summary>
    /// 病房
    /// </summary>
    public string Ward { get; set; }
    /// <summary>
    /// 病床
    /// </summary>
    public string SickBed { get; set; }
    /// <summary>
    /// 诊断结果
    /// </summary>
    public string DiagResult { get; set; }
    /// <summary>
    /// 取药时间
    /// </summary>
    public DateTime? GetDrugTime { get; set; }
    /// <summary>
    /// 取药序号
    /// </summary>
    public string GetDrugNum { get; set; }
    /// <summary>
    /// 处方接收时间
    /// </summary>
    public DateTime? DoTime { get; set; }
    /// <summary>
    /// 快递类型
    /// </summary>
    public string DtbType { get; set; }
    /// <summary>
    /// 标签数量
    /// </summary>
    public int LabelNum { get; set; }
    /// <summary>
    /// 医生
    /// </summary>
    public string Doctor { get; set; }
    /// <summary>
    /// 脚注
    /// </summary>
    public string Footnote { get; set; }
    /// <summary>
    /// 服用方法
    /// </summary>
    public string Takeway { get; set; }
    /// <summary>
    /// 服用方式
    /// </summary>
    public string TakeMethod { get; set; }
    /// <summary>
    /// 金额
    /// </summary>
    public decimal Money { get; set; }
    /// <summary>
    /// 序号
    /// </summary>
    public string OutpatientIndex { get; set; }
    /// <summary>
    /// 是否走全人工补配
    /// </summary>
    public int IsManual { get; set; }
    /// <summary>
    /// 加工类型
    /// </summary>
    public string Processtype { get; set; }

    /// <summary>
    /// 药品明细
    /// </summary>
    public List<DDCSForDongChengDrug> DrugList { get; set; }
}
public class DDCSForDongChengDrug
{
    /// <summary>
    /// 药品名称（本厂）
    /// </summary>
    public string DrugName { get; set; }
    /// <summary>
    /// 药品编码唯一（本厂）
    /// </summary>
    public string DrugNum { get; set; }
    /// <summary>
    /// 医院药品编码
    /// </summary>
    public string HospitalDrugCode { get; set; }
    /// <summary>
    /// 医院药品名称
    /// </summary>
    public string HospitalDrugName { get; set; }
    /// <summary>
    /// 规格
    /// </summary>
    public string DrugPosition { get; set; }
    /// <summary>
    /// 单帖量
    /// </summary>
    public decimal DrugallNum { get; set; }
    /// <summary>
    /// 品脚注（先煎 后下 包煎 冲服 打粉 打碎 粉吞 久浸 口服 另包 另包自煎 另大包 浓煎 水煎 吞服 外洗 外用 烊冲）
    /// </summary>
    public string DrugDescription { get; set; }
    /// <summary>
    /// 贴数
    /// </summary>
    public int Dose { get; set; }
    /// <summary>
    /// 药物重量 单帖量*贴数
    /// </summary>
    public decimal DrugWeight { get; set; }
    /// <summary>
    /// 说明
    /// </summary>
    public string Description { get; set; }
    /// <summary>
    /// 单价
    /// </summary>
    public decimal RetailPrice { get; set; }
    /// <summary>
    /// 单位：g、条、只、克
    /// </summary>
    public string Unit { get; set; }
    /// <summary>
    /// 柜号
    /// </summary>
    public string BoxNum { get; set; }
}



