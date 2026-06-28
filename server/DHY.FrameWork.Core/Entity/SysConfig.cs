namespace DHY.Core;

/// <summary>
/// 系统参数配置表
/// </summary>
[SugarTable(null, "系统参数配置表")]
[SysTable]
[SugarIndex("index_{table}_N", nameof(Name), OrderByType.Asc)]
[SugarIndex("index_{table}_C", nameof(Code), OrderByType.Asc)]
public partial class SysConfig : EntityBase
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnDescription = "名称", Length = 64)]
    [Required, MaxLength(64)]
    public virtual string Name { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnDescription = "编码", Length = 64)]
    [MaxLength(64)]
    public string? Code { get; set; }

    /// <summary>
    /// 属性值
    /// </summary>
    [SugarColumn(ColumnDescription = "属性值", Length = 2000)]
    [MaxLength(2000)]
    public string? Value { get; set; }

    /// <summary>
    /// 是否是内置参数（Y-是，N-否）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否是内置参数")]
    public YesNoEnum SysFlag { get; set; }

    /// <summary>
    /// 分组编码
    /// </summary>
    [SugarColumn(ColumnDescription = "分组编码", Length = 64)]
    [MaxLength(64)]
    public string? GroupCode { get; set; }



    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 256)]
    [MaxLength(256)]
    public string? Remark { get; set; }
}