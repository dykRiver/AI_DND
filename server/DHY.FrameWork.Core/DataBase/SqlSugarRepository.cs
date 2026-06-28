namespace DHY.Core
{
    /// <summary>
    /// SqlSugar 实体仓储
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SqlSugarRepository<T> : SimpleRepository<T>, IScoped where T : class, new()
    {
        public SqlSugarRepository() : base()
        {
            // 若实体贴有系统表特性，则返回默认库连接
            if (typeof(T).IsDefined(typeof(SysTableAttribute), false))
                return;

            // 若未贴任何表特性或当前未登录或是默认租户Id，则返回默认库连接
            var tenantId = App.User?.FindFirst(ClaimConst.TenantId)?.Value;
            if (string.IsNullOrWhiteSpace(tenantId) || tenantId == SqlSugarConst.MainConfigId) return;

            // 根据租户Id切换库连接, 为空则返回默认库连接
            var sqlSugarScopeProviderTenant = App.GetRequiredService<SysTenantService>().GetTenantDbConnectionScope(long.Parse(tenantId));
            if (sqlSugarScopeProviderTenant == null) return;
            Context = sqlSugarScopeProviderTenant;
        }
    }
}
