namespace DHY.InternalApiService.Dtos;

public class SysConfigOutput
{
    /// <summary>
    /// 名称
    /// </summary>
    public virtual string Name { get; set; }

    /// <summary>
    /// 编码
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// 属性值
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// 分组编码
    /// </summary>
    public string? GroupCode { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
