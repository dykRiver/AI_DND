using DHY.Core.Dto;
using Furion;
using Furion.EventBus;
using Furion.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DHY.Core;

public static class LoggingSetup
{
    /// <summary>
    /// 日志注册
    /// </summary>
    /// <param name="services"></param>
    public static void AddLoggingSetup(this IServiceCollection services)
    {
        string[] ignoreLoggingNames = ["WorkflowCore.Services.BackgroundTasks.RunnablePoller",
            "Microsoft.Extensions.Http.DefaultHttpClientFactory",
            "System.Net.Http.HttpClient.default.ClientHandler",
            "System.Net.Http.HttpClient.default.LogicalHandler",
            "MqttDispatcher"
            ];
        string[] ignoreWarningLoggingNames = ["System.Logging.EventBusService"];
        // 日志监听
        services.AddMonitorLogging(options =>
        {
            options.IgnorePropertyNames = ["Byte"];
            options.IgnorePropertyTypes = [typeof(byte[])];
        });

        // 控制台日志
        var consoleLog = App.GetConfig<bool>("Logging:Monitor:ConsoleLog", true);

        services.AddConsoleFormatter(options =>
        {
            //options.DateFormat = "yyyy-MM-dd HH:mm:ss:ffff(zzz) dddd";
            options.DateFormat = "yyyy-MM-dd HH:mm:ss:ffff dddd";
            //options.WithTraceId = true; // 显示线程Id
            //options.WithStackFrame = true; // 显示程序集
            options.WriteFilter = (logMsg) =>
            {
                if ((logMsg.LogLevel == LogLevel.Error || logMsg.LogLevel == LogLevel.Warning) && !ignoreWarningLoggingNames.Contains(logMsg.LogName))
                {
                    var pushMessage = logMsg.Message;

                    if (pushMessage.StartsWith("w="))
                    {
                        var startIndex = pushMessage.IndexOf("容器【");
                        pushMessage = pushMessage.Substring(startIndex, pushMessage.Length - startIndex);
                    }

                    MessageCenter.PublishAsync("LogEvent_OutputWarningAndErrorMessages", new LogEventPayload()
                    {
                        Message = pushMessage,
                        LogLevel = logMsg.LogLevel,
                        LogDateTime = logMsg.LogDateTime,
                        LogName = logMsg.LogName,
                        Context = logMsg.Context,
                        EventId = logMsg.EventId,
                        Exception = logMsg.Exception,
                        State = logMsg.State,
                        TraceId = logMsg.TraceId,
                        ThreadId = logMsg.ThreadId,
                        UseUtcTimestamp = logMsg.UseUtcTimestamp
                    });
                    return true;
                }
                if (!consoleLog || ignoreLoggingNames.Contains(logMsg.LogName))
                {
                    return false;
                }

                return consoleLog;
            };
        });

        // 日志写入文件
        if (App.GetConfig<bool>("Logging:File:Enabled", true))
        {
            var loggingMonitorSettings = App.GetConfig<LoggingMonitorSettings>("Logging:Monitor", true);
            Array.ForEach(new[] { LogLevel.Trace, LogLevel.Debug, LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical }, logLevel =>
            {
                services.AddFileLogging(options =>
                {
                    options.WithTraceId = true; // 显示线程Id
                    options.WithStackFrame = false; // 显示程序集
                    options.FileNameRule = fileName => string.Format(fileName, DateTime.Now, logLevel.ToString()); // 每天创建一个文件
                    options.WriteFilter = (logMsg) =>
                    {
                        if (ignoreLoggingNames.Contains(logMsg.LogName) || ignoreWarningLoggingNames.Contains(logMsg.LogName))
                        {
                            return false;
                        }

                        return logMsg.LogLevel == logLevel;
                    }; //日志级别
                    options.HandleWriteError = (writeError) => // 写入失败时启用备用文件
                    {
                        writeError.UseRollbackFileName(Path.GetFileNameWithoutExtension(writeError.CurrentFileName) + "-oops" + Path.GetExtension(writeError.CurrentFileName));
                    };

                    if (loggingMonitorSettings.JsonBehavior == JsonBehavior.OnlyJson)
                    {
                        options.MessageFormat = LoggerFormatter.Json;
                        // options.MessageFormat = LoggerFormatter.JsonIndented;
                        options.MessageFormat = (logMsg) =>
                        {
                            var jsonString = logMsg.Context.Get("loggingMonitor");
                            return jsonString?.ToString();
                        };
                    }
                });
            });
        }

    }
}