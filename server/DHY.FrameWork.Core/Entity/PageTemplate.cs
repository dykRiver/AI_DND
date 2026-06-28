namespace DHY.Core;

/// <summary>
/// 系统菜单表
/// </summary>
[SugarTable(null, "模板表")]
public class PageTemplate : EntityBase
{
    /// <summary>
    /// 名称
    /// </summary>
    [SugarColumn(ColumnDescription = "名称", Length = 64)]
    [Required, MaxLength(64)]
    public string Name { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    [SugarColumn(ColumnDescription = "编码", Length = 64)]
    [MaxLength(64)]
    public string Code { get; set; }

    /// <summary>
    /// 菜单ID
    /// </summary>
    [SugarColumn(ColumnDescription = "挂载菜单ID")]
    public long MenuId { get; set; }

    /// <summary>
    /// 说明
    /// </summary>
    [SugarColumn(ColumnDescription = "说明", Length = 256)]
    [MaxLength(256)]
    public string? Description { get; set; }


    /// <summary>
    /// 分类Id
    /// </summary>
    [SugarColumn(ColumnDescription = "数据连接Id")]
    public long DataSroucesId2 { get; set; }


    /// <summary>
    /// 说明
    /// </summary>
    ///[SugarColumn(ColumnDescription = "模板配置", 大文本)]
    [SugarColumn(ColumnDescription = "模板配置", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string TemplateConfig { get; set; }


    [SugarColumn(DefaultValue = "true")]
    public bool Enabled { get; set; }
}
