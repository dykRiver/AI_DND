namespace DHY.FrameWork.Core.DataBase
{
    /// <summary>
    /// 数据库操作AOP
    /// </summary>
    public class DataBaseFilter
    {
        public static void ConnectionFilter(SqlSugarScopeProvider sqlSugarScopeProvider, DbConnectionConfig config)
        {
            // 获取默认库连接配置
            var dbOptions = App.GetOptions<DbConnectionOptions>();
            SetDbAop(sqlSugarScopeProvider, dbOptions.EnableConsoleSql);
            SetDbDiffLog(sqlSugarScopeProvider, config);

        }
        /// <summary>
        /// 配置Aop
        /// </summary>
        /// <param name="db"></param>
        /// <param name="enableConsoleSql"></param>
        public static void SetDbAop(SqlSugarScopeProvider db, bool enableConsoleSql)
        {
            // 设置超时时间
            db.Ado.CommandTimeOut = 30;

            // 打印SQL语句
            if (enableConsoleSql)
            {
                db.Aop.OnLogExecuting = (sql, pars) =>
                {
                    var log = $"【{DateTime.Now}——执行SQL】{Environment.NewLine}{UtilMethods.GetNativeSql(sql, pars)}{Environment.NewLine}";
                    var originColor = Console.ForegroundColor;
                    if (sql.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
                        Console.ForegroundColor = ConsoleColor.Green;
                    if (sql.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) || sql.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase))
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    if (sql.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                        Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(log);
                    Console.ForegroundColor = originColor;
                    App.PrintToMiniProfiler("SqlSugar", "Info", log);
                };
                db.Aop.OnError = ex =>
                {
                    if (ex.Parametres == null) return;
                    var log = $"【{DateTime.Now}——错误SQL】{Environment.NewLine}{UtilMethods.GetNativeSql(ex.Sql, (SugarParameter[])ex.Parametres)}{Environment.NewLine}";
                    var originColor = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(log);
                    Console.ForegroundColor = originColor;
                    App.PrintToMiniProfiler("SqlSugar", "Error", log);
                };
                db.Aop.OnLogExecuted = (sql, pars) =>
                {
                    // 执行时间超过5秒时
                    if (db.Ado.SqlExecutionTime.TotalSeconds > 5)
                    {
                        var fileName = db.Ado.SqlStackTrace.FirstFileName; // 文件名
                        var fileLine = db.Ado.SqlStackTrace.FirstLine; // 行号
                        var firstMethodName = db.Ado.SqlStackTrace.FirstMethodName; // 方法名
                        var log = $"【{DateTime.Now}——超时SQL】{Environment.NewLine}【所在文件名】：{fileName}{Environment.NewLine}【代码行数】：{fileLine}{Environment.NewLine}【方法名】：{firstMethodName}{Environment.NewLine}" + $"【SQL语句】：{UtilMethods.GetNativeSql(sql, pars)}";
                        var originColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(log);
                        Console.ForegroundColor = originColor;
                        App.PrintToMiniProfiler("SqlSugar", "Slow", log);
                    }
                };
            }
            // 数据审计

            db.Aop.DataExecuting = (oldValue, entityInfo) =>
            {
                if (entityInfo.OperationType == DataFilterType.InsertByObject)
                {
                    // 主键(long类型)且没有值的---赋值雪花Id
                    if (entityInfo.EntityColumnInfo.IsPrimarykey && entityInfo.EntityColumnInfo.PropertyInfo.PropertyType == typeof(long))
                    {
                        var id = entityInfo.EntityColumnInfo.PropertyInfo.GetValue(entityInfo.EntityValue);
                        if (id == null || (long)id == 0)
                            entityInfo.SetValue(YitIdHelper.NextId());
                    }

                    if (entityInfo.PropertyName == nameof(EntityBase.CreateTime))
                        entityInfo.SetValue(DateTime.Now);

                    if (App.User != null)
                    {
                        if (entityInfo.PropertyName == nameof(EntityTenantId.TenantId))
                        {
                            var tenantId = ((dynamic)entityInfo.EntityValue).TenantId;
                            if (tenantId == null || tenantId == 0)
                                entityInfo.SetValue(App.User.FindFirst(ClaimConst.TenantId)?.Value);
                        }
                        else if (entityInfo.PropertyName == nameof(EntityBase.CreateUserId))
                        {
                            var createUserId = ((dynamic)entityInfo.EntityValue).CreateUserId;
                            if (createUserId == 0 || createUserId == null)
                                entityInfo.SetValue(App.User.FindFirst(ClaimConst.UserId)?.Value);
                        }
                        else if (entityInfo.PropertyName == nameof(EntityBase.CreateUserName))
                        {
                            var createUserName = ((dynamic)entityInfo.EntityValue).CreateUserName;
                            if (string.IsNullOrEmpty(createUserName))
                                entityInfo.SetValue(App.User.FindFirst(ClaimConst.RealName)?.Value);
                        }
                        else if (entityInfo.PropertyName == nameof(EntityBaseData.CreateOrgId))
                        {
                            var createOrgId = ((dynamic)entityInfo.EntityValue).CreateOrgId;
                            if (createOrgId == 0 || createOrgId == null)
                                entityInfo.SetValue(App.User.FindFirst(ClaimConst.OrgId)?.Value);
                        }
                        else if (entityInfo.PropertyName == nameof(EntityBaseData.CreateOrgName))
                        {
                            var createOrgName = ((dynamic)entityInfo.EntityValue).CreateOrgName;
                            if (string.IsNullOrEmpty(createOrgName))
                                entityInfo.SetValue(App.User.FindFirst(ClaimConst.OrgName)?.Value);
                        }
                    }
                }
                if (entityInfo.OperationType == DataFilterType.UpdateByObject)
                {
                    if (entityInfo.PropertyName == nameof(EntityBase.UpdateTime))
                        entityInfo.SetValue(DateTime.Now);
                    else if (entityInfo.PropertyName == nameof(EntityBase.UpdateUserId))
                        entityInfo.SetValue(App.User?.FindFirst(ClaimConst.UserId)?.Value);
                    else if (entityInfo.PropertyName == nameof(EntityBase.UpdateUserName))
                        entityInfo.SetValue(App.User?.FindFirst(ClaimConst.RealName)?.Value);
                }
            };

            // 超管排除其他过滤器
            if (App.User?.FindFirst(ClaimConst.AccountType)?.Value == ((int)AccountTypeEnum.SuperAdmin).ToString())
                return;

            // 配置假删除过滤器
            db.QueryFilter.AddTableFilter<IDeletedFilter>(u => u.IsDelete == false);

            // 配置租户过滤器
            var tenantId = App.User?.FindFirst(ClaimConst.TenantId)?.Value;
            if (!string.IsNullOrWhiteSpace(tenantId))
                db.QueryFilter.AddTableFilter<ITenantIdFilter>(u => u.TenantId == long.Parse(tenantId));

            // 配置用户机构（数据范围）过滤器
            SqlSugarFilter.SetOrgEntityFilter(db);

            // 配置自定义过滤器
            SqlSugarFilter.SetCustomEntityFilter(db);
        }


        /// <summary>
        /// 开启库表差异化日志
        /// </summary>
        /// <param name="db"></param>
        /// <param name="config"></param>
        private static void SetDbDiffLog(SqlSugarScopeProvider db, DbConnectionConfig config)
        {
            if (!config.DbSettings.EnableDiffLog) return;

            db.Aop.OnDiffLogEvent = async u =>
            {
                var logDiff = new SysLogDiff
                {
                    // 操作后记录（字段描述、列名、值、表名、表描述）
                    AfterData = JSON.Serialize(u.AfterData),
                    // 操作前记录（字段描述、列名、值、表名、表描述）
                    BeforeData = JSON.Serialize(u.BeforeData),
                    // 传进来的对象
                    BusinessData = JSON.Serialize(u.BusinessData),
                    // 枚举（insert、update、delete）
                    DiffType = u.DiffType.ToString(),
                    Sql = UtilMethods.GetNativeSql(u.Sql, u.Parameters),
                    Parameters = JSON.Serialize(u.Parameters),
                    Elapsed = u.Time == null ? 0 : (long)u.Time.Value.TotalMilliseconds
                };
                await db.CopyNew().Insertable(logDiff).ExecuteCommandAsync();
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(DateTime.Now + $"{Environment.NewLine}*****开始差异日志*****{Environment.NewLine}{Environment.NewLine}{JSON.Serialize(logDiff)}{Environment.NewLine}*****结束差异日志*****{Environment.NewLine}");
            };
        }
        /// <summary>
        /// 初始化租户业务数据库
        /// </summary>
        /// <param name="iTenant"></param>
        /// <param name="config"></param>
        /// <param name="cacheService"></param>
        public static void InitTenantDatabase(ITenant iTenant, DbConnectionConfig config, ICacheService cacheService)
        {
            SqlSugarSetup.SetDbConfig(config, cacheService);

            if (!iTenant.IsAnyConnection(config.ConfigId.ToString()))
                iTenant.AddConnection(config);
            var db = iTenant.GetConnectionScope(config.ConfigId.ToString());
            db.DbMaintenance.CreateDatabase();

            // 获取所有业务表-初始化租户库表结构（排除系统表、日志表、特定库表）
            var entityTypes = App.EffectiveTypes.Where(u => !u.IsInterface && !u.IsAbstract && u.IsClass && u.IsDefined(typeof(SugarTable), false) &&
                !u.IsDefined(typeof(SysTableAttribute), false) && !u.IsDefined(typeof(LogTableAttribute), false) && !u.IsDefined(typeof(TenantAttribute), false)).ToList();
            if (!entityTypes.Any()) return;

            foreach (var entityType in entityTypes)
            {
                var splitTable = entityType.GetCustomAttribute<SplitTableAttribute>();
                if (splitTable == null)
                    db.CodeFirst.InitTables(entityType);
                else
                    db.CodeFirst.SplitTables().InitTables(entityType);
            }
        }


    }
}
