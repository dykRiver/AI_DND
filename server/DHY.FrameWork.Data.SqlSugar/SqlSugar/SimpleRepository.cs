using SqlSugar;

namespace DHY.Core;

/// <summary>
/// SqlSugar 实体仓储
/// </summary>
/// <typeparam name="T"></typeparam>
public class SimpleRepository<T> : SimpleClient<T> where T : class, new()
{
    public SimpleRepository()
    {
        var iTenant = SqlSugarSetup.ITenant; // App.GetRequiredService<ISqlSugarClient>().AsTenant();
        base.Context = iTenant.GetConnectionScope(SqlSugarConst.MainConfigId);

        // 若实体贴有多库特性，则返回指定库连接
        if (typeof(T).IsDefined(typeof(TenantAttribute), false))
        {
            base.Context = iTenant.GetConnectionScopeWithAttr<T>();
            return;
        }

        // 若实体贴有日志表特性，则返回日志库连接
        if (typeof(T).IsDefined(typeof(LogTableAttribute), false))
        {
            if (iTenant.IsAnyConnection(SqlSugarConst.LogConfigId))
                base.Context = iTenant.GetConnectionScope(SqlSugarConst.LogConfigId);
            return;
        }

    }
}