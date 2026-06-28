using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Furion;
using Microsoft.Extensions.DependencyInjection;

namespace DHY.Core.Extensions;

public static class ServeExt
{
    public static async Task RunApplication(params string[] args)
    {
        await Serve.RunNativeAsync(includeWeb: false, args: @args);
        RunApplicationEntry();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Program running...");
        Console.ResetColor();
    }

    /// <summary>
    /// 启动原生应用
    /// </summary>
    private static void RunApplicationEntry()
    {
        // 扫描所有继承 DDCSStartup 的类
        var startups = App.EffectiveTypes
            .Where(u => typeof(DDCSStartup).IsAssignableFrom(u) && u.IsClass && !u.IsAbstract && !u.IsGenericType)
            .OrderByDescending(GetStartupOrder);

        foreach (var type in startups)
        {
            var startup = Activator.CreateInstance(type) as DDCSStartup;

            // 获取所有符合返回值void或Task，且方法标记了NativeApplicationEntry。
            var serviceMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(u => (u.ReturnType == typeof(void) || u.ReturnType == typeof(Task))
                && u.GetCustomAttribute<NativeApplicationEntryAttribute>() != null);

            if (!serviceMethods.Any())
            {
                continue;
            }

            // 自动安装属性调用
            foreach (var method in serviceMethods)
            {
                var methodParams = method.GetParameters();
                object[] paramList = methodParams.Select(p => App.GetService(p.ParameterType)).ToArray();
                var attrSettings = method.GetCustomAttribute<NativeApplicationEntryAttribute>();

                if (method.ReturnType == typeof(void) && method.Name != "RunApplication")
                {
                    throw new NotSupportedException("暂不支持自定义启动方法名称，请使用 public void RunApplication");
                }
                else if (method.ReturnType == typeof(Task) && method.Name != "RunApplicationAsync")
                {
                    throw new NotSupportedException("暂不支持自定义启动方法名称，请使用 public [async] Task RunApplicationAsync");
                }

                if (attrSettings.RunNewThread)
                {
                    Task.Run(() => { method.Invoke(startup, paramList.ToArray()); });
                }
                else
                {
                    var methodReturn = method.Invoke(startup, paramList.ToArray());

                    if (methodReturn is Task t)
                    {
                        t.GetAwaiter().GetResult();
                    }
                    else if (methodReturn is ValueTask tv)
                    {
                        tv.GetAwaiter().GetResult();
                    }
                }
            }
        }

        static int GetStartupOrder(Type type)
        {
            return !type.IsDefined(typeof(AppStartupAttribute), true) ? 0 : type.GetCustomAttribute<AppStartupAttribute>(true).Order;
        }

    }

    public static IServiceCollection AddDhyDefaultSettings(this IServiceCollection services)
    {
        // 控制台logo
        services.AddConsoleLogo();
        services.AddConsoleFormatter(options =>
        {
            options.WithTraceId = true;
        });

        services.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.AddDateTimeTypeConverters("yyyy-MM-dd HH:mm:ss");
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.IncludeFields = true;
            options.JsonSerializerOptions.AllowTrailingCommas = true;
            options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
        });

        return services;
    }
}
