namespace DHY.InternalApiService;

public sealed class PackerLabelRollDto
{
    public long Id { get; set; }
    /// <summary>
    /// 包装卷名称
    /// </summary>
    public string LableRollName { get; set; }

    /// <summary>
    /// 包装卷类型
    /// </summary>
    public int LableRollType { get; set; }

    /// <summary>
    /// 规格
    /// </summary>
    public int Specification { get; set; }

    /// <summary>
    /// 容积
    /// </summary>
    public int Volume { get; set; }
}
