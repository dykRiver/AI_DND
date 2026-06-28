using System.ComponentModel.DataAnnotations;

///<summary>
/// 煎药系统配置
///</summary>
[SugarTable("sysKeyValue")]
[SugarIndex("index_{table}_ParentID", nameof(ParentID), OrderByType.Desc)]
public sealed class SysKeyValue : EntityTenant
{
    /// <summary>
    /// 数据字典父ID
    /// </summary>
    [SugarColumn(ColumnDescription = "数据字典父ID")]
    [Required]
    public int ParentID { get; set; }

    /// <summary>
    /// 数据字典类别 1：组织结构
    /// </summary>
    [SugarColumn(ColumnDescription = "数据字典类别")]
    [Required]
    public int KType { get; set; }

    /// <summary>
    /// 数据字典名称
    /// </summary>
    [SugarColumn(ColumnDescription = "数据字典名称", Length = 100)]
    public string KName { get; set; }

    /// <summary>
    /// 数据字典值
    /// </summary>
    [SugarColumn(ColumnDescription = "数据字典值", Length = 500)]
    public string Kvalue { get; set; }

    [SugarColumn(IsNullable = true)]
    public int? IsCheck { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 100)]
    public string KRemark { get; set; }

    /// <summary>
    /// 云平台编码
    /// </summary>       
    [SugarColumn(ColumnName = "cloudplatformcode", ColumnDescription = "云平台编码", Length = 100)]
    public string CloudPlatformCode { get; set; }

    /// <summary>
    /// 云平台描述
    /// </summary>       
    [SugarColumn(ColumnName = "cloudplatformdescribe", ColumnDescription = "云平台描述", Length = 100)]
    public string CloudPlatformDescribe { get; set; }
}