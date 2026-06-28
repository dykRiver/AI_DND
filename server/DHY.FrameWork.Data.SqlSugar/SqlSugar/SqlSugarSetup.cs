using System.Collections;
using System.Reflection;
using Furion;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using Yitter.IdGenerator;

namespace DHY.Core;

public static class SqlSugarSetup
{
    // 多租户实例
    public static ITenant ITenant { get; set; }

    /// <summary>
    /// SqlSugar 上下文初始化
    /// </summary>
    /// <param name="services"></param>
    public static void AddSqlSugar(this IServiceCollection services, ICacheService cacheService, Action<SqlSugarScopeProvider, DbConnectionConfig> connectionAction)
    {
        //导航插入时需要替换自定义的雪花Id
        StaticConfig.CustomSnowFlakeFunc = YitIdHelper.NextId;

        var dbOptions = App.GetConfig<DbConnectionOptions>("DbConnection", true);
        dbOptions.ConnectionConfigs.ForEach(s => SetDbConfig(s, cacheService));

        SqlSugarScope sqlSugar = new(dbOptions.ConnectionConfigs.Adapt<List<ConnectionConfig>>(), db =>
        {
            dbOptions.ConnectionConfigs.ForEach(config =>
            {

                var dbProvider = db.GetConnectionScope(config.ConfigId);
                config.IsAutoCloseConnection = true;
                connectionAction?.Invoke(dbProvider, config);
            });
        });
        ITenant = sqlSugar;

        services.AddSingleton<ISqlSugarClient>(sqlSugar); // 单例注册
        services.AddScoped(typeof(SimpleRepository<>)); // 仓储注册
        services.AddUnitOfWork<SqlSugarUnitOfWork>(); // 事务与工作单元注册

        //DbFirst 生成实体对象
        //var provider = sqlSugar.GetConnection(SqlSugarConst.DecoctingSystemConfigId);
        //provider.DbFirst.Where("prescription").CreateClassFile("D:\\dhySrc\\ddcsmain\\DHY.DDCS.Prescription\\Entities\\Compatible", "Prescription");
        //var provider = sqlSugar.GetConnection(SqlSugarConst.DecoctingSystemConfigId);
        //provider.DbFirst.Where("Drug").CreateClassFile("D:\\dhySrc\\ddcsmain\\DHY.DDCS.Prescription\\Entities\\Compatible", "Drug");
        // 初始化数据库表结构及种子数据
        dbOptions.ConnectionConfigs.ForEach(config =>
        {
            InitDatabase(sqlSugar, config);
        });
    }

    /// <summary>
    /// 配置连接属性
    /// </summary>
    /// <param name="config"></param>
    public static void SetDbConfig(DbConnectionConfig config, ICacheService cacheService)
    {
        var configureExternalServices = new ConfigureExternalServices
        {
            EntityNameService = (type, entity) => // 处理表
            {
                entity.IsDisabledDelete = true; // 禁止删除非 sqlsugar 创建的列
                // 只处理贴了特性[SugarTable]表
                if (!type.GetCustomAttributes<SugarTable>().Any())
                    return;
                if (config.DbSettings.EnableUnderLine && !entity.DbTableName.Contains('_'))
                    entity.DbTableName = UtilMethods.ToUnderLine(entity.DbTableName); // 驼峰转下划线
            },
            EntityService = (type, column) => // 处理列
            {
                // 只处理贴了特性[SugarColumn]列
                if (!type.GetCustomAttributes<SugarColumn>().Any())
                    return;
                if (new NullabilityInfoContext().Create(type).WriteState is NullabilityState.Nullable)
                    column.IsNullable = true;
                if (config.DbSettings.EnableUnderLine && !column.IsIgnore && !column.DbColumnName.Contains('_'))
                    column.DbColumnName = UtilMethods.ToUnderLine(column.DbColumnName); // 驼峰转下划线
            },
            DataInfoCacheService = cacheService,
        };
        config.ConfigureExternalServices = configureExternalServices;
        config.InitKeyType = InitKeyType.Attribute;
        config.IsAutoCloseConnection = true;
        config.MoreSettings = new ConnMoreSettings
        {
            IsAutoRemoveDataCache = true, // 启用自动删除缓存，所有增删改会自动调用.RemoveDataCache()
            IsAutoDeleteQueryFilter = true, // 启用删除查询过滤器
            IsAutoUpdateQueryFilter = true, // 启用更新查询过滤器
            SqlServerCodeFirstNvarchar = true // 采用Nvarchar
        };
    }





    /// <summary>
    /// 初始化数据库
    /// </summary>
    /// <param name="db"></param>
    /// <param name="config"></param>
    private static void InitDatabase(SqlSugarScope db, DbConnectionConfig config)
    {
        SqlSugarScopeProvider dbProvider = db.GetConnectionScope(config.ConfigId);

        // 初始化/创建数据库
        if (config.DbSettings.EnableInitDb)
        {
            if (config.DbType != SqlSugar.DbType.Oracle)
                dbProvider.DbMaintenance.CreateDatabase();
        }

        // 初始化表结构
        if (config.TableSettings.EnableInitTable)
        {
            var entityTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass && u.IsDefined(typeof(SugarTable), false))
                .WhereIF(config.TableSettings.EnableIncreTable, u => u.IsDefined(typeof(IncreTableAttribute), false)).ToList();

            if (config.ConfigId.ToString() == SqlSugarConst.MainConfigId) // 默认库（有系统表特性、没有日志表和租户表特性）
                entityTypes = entityTypes.Where(u => u.GetCustomAttributes<SysTableAttribute>().Any() || (!u.GetCustomAttributes<LogTableAttribute>().Any() && !u.GetCustomAttributes<TenantAttribute>().Any())).ToList();
            else if (config.ConfigId.ToString() == SqlSugarConst.LogConfigId) // 日志库
                entityTypes = entityTypes.Where(u => u.GetCustomAttributes<LogTableAttribute>().Any()).ToList();
            else
                entityTypes = entityTypes.Where(u => u.GetCustomAttribute<TenantAttribute>()?.configId.ToString() == config.ConfigId.ToString()).ToList(); // 自定义的库

            foreach (var entityType in entityTypes)
            {
                if (entityType.GetCustomAttribute<SplitTableAttribute>() == null)
                    dbProvider.CodeFirst.InitTables(entityType);
                else
                    dbProvider.CodeFirst.SplitTables().InitTables(entityType);
            }
        }

        // 初始化种子数据
        if (config.SeedSettings.EnableInitSeed)
        {
            var seedDataTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass && u.GetInterfaces().Any(i => i.HasImplementedRawGeneric(typeof(ISqlSugarEntitySeedData<>))))
                .WhereIF(config.SeedSettings.EnableIncreSeed, u => u.IsDefined(typeof(IncreSeedAttribute), false)).ToList();

            foreach (var seedType in seedDataTypes)
            {
                var entityType = seedType.GetInterfaces().First().GetGenericArguments().First();
                if (config.ConfigId.ToString() == SqlSugarConst.MainConfigId) // 默认库（有系统表特性、没有日志表和租户表特性）
                {
                    if (entityType.GetCustomAttribute<SysTableAttribute>() == null && (entityType.GetCustomAttribute<LogTableAttribute>() != null || entityType.GetCustomAttribute<TenantAttribute>() != null))
                        continue;
                }
                else if (config.ConfigId.ToString() == SqlSugarConst.LogConfigId) // 日志库
                {
                    if (entityType.GetCustomAttribute<LogTableAttribute>() == null)
                        continue;
                }
                else
                {
                    var att = entityType.GetCustomAttribute<TenantAttribute>(); // 自定义的库
                    if (att == null || att.configId.ToString() != config.ConfigId.ToString()) continue;
                }

                var instance = Activator.CreateInstance(seedType);
                var hasDataMethod = seedType.GetMethod("HasData");
                var seedData = ((IEnumerable)hasDataMethod?.Invoke(instance, null))?.Cast<object>();
                if (seedData == null) continue;

                var entityInfo = dbProvider.EntityMaintenance.GetEntityInfo(entityType);
                if (entityInfo.Columns.Any(u => u.IsPrimarykey))
                {
                    // 按主键进行批量增加和更新
                    var storage = dbProvider.StorageableByObject(seedData.ToList()).ToStorage();
                    storage.AsInsertable.ExecuteCommand();
                    // 暂不更新已有数据
                    //storage.AsUpdateable.ExecuteCommand();
                }
                else
                {
                    // 无主键则只进行插入
                    if (!dbProvider.Queryable(entityInfo.DbTableName, entityInfo.DbTableName).Any())
                        dbProvider.InsertableByObject(seedData.ToList()).ExecuteCommand();
                }
            }
        }
    }


}