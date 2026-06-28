using SqlSugar;

namespace DHY.Core;

/// <summary>
/// 租户实体基类 + 业务数据（数据权限）
/// </summary>
public abstract class EntityTenantBaseData : EntityBaseData, ITenantIdFilter
{
    /// <summary>
    /// 租户Id
    /// </summary>
    [SugarColumn(ColumnDescription = "租户Id", IsOnlyIgnoreUpdate = true)]
    public virtual long? TenantId { get; set; }
}