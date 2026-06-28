/// <summary>
/// VForm 树形组件数据源模型
/// </summary>
public class TreeViewModel
{
    /// <summary>
    /// 数据Id
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 标签展示文字
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// 附加描述信息（不展示，可作为查询条件）
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 子节点
    /// </summary>
    public List<TreeViewModel> Children { get; set; }

    /// <summary>
    /// 父节点Id
    /// </summary>
    public long ParentId { get; set; }
}