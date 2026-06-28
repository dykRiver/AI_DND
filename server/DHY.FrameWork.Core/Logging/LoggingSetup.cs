namespace DHY.FrameWork.Core.Logging;

public static class LoggingSetupExtension
{

    /// <summary>
    /// 注册数据库日志写入
    /// </summary>
    /// <param name="services"></param>
    public static void AddDataBaseLogging(this IServiceCollection services)
    {

        // 日志写入数据库
        if (App.GetConfig<bool>("Logging:Database:Enabled", true))
        {
            services.AddDatabaseLogging<DatabaseLoggingWriter>(options =>
            {
                options.WithTraceId = true; // 显示线程Id
                options.WithStackFrame = true; // 显示程序集
                options.IgnoreReferenceLoop = false; // 忽略循环检测
                options.WriteFilter = (logMsg) =>
                {
                    return logMsg.LogName == "System.Logging.LoggingMonitor"; // 只写LoggingMonitor日志
                };
            });
        }


    }
    /// <summary>
    /// 注册ElasticSearch日志写入
    /// </summary>
    /// <param name="services"></param>
    public static void AddDElasticSearchLogging(this IServiceCollection services)
    {
        // 日志写入ElasticSearch
        if (App.GetConfig<bool>("Logging:ElasticSearch:Enabled", true))
        {
            services.AddDatabaseLogging<ElasticSearchLoggingWriter>(options =>
            {
                options.WithTraceId = true; // 显示线程Id
                options.WithStackFrame = true; // 显示程序集
                options.IgnoreReferenceLoop = false; // 忽略循环检测
                options.MessageFormat = LoggerFormatter.Json;
                options.WriteFilter = (logMsg) =>
                {
                    return logMsg.LogName == "System.Logging.LoggingMonitor"; // 只写LoggingMonitor日志
                };
            });
        }
    }
}