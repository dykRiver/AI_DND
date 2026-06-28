namespace DHY.Core;

/// <summary>
/// 系统动态插件表
/// </summary>
[SugarTable(null, "系统动态插件表")]
[SysTable]
[SugarIndex("index_{table}_N", nameof(Name), OrderByType.Asc)]
public partial class SysPlugin : EntityTenant
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnDescription = "名称", Length = 64)]
    [Required, MaxLength(64)]
    public virtual string Name { get; set; }

    /// <summary>
    /// C#代码
    /// </summary>
    [SugarColumn(ColumnDescription = "C#代码", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    [Required]
    public virtual string CsharpCode { get; set; }

    /// <summary>
    /// 程序集名称
    /// </summary>
    [SugarColumn(ColumnDescription = "程序集名称", Length = 512)]
    [MaxLength(512)]
    public string? AssemblyName { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public StatusEnum Status { get; set; } = StatusEnum.Enable;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 128)]
    [MaxLength(128)]
    public string? Remark { get; set; }
}