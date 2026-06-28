using System;
using AspNetCoreRateLimit;
using DHY.Core;
using DHY.Core.EventBus;
using DHY.Core.Extensions;
using DHY.Core.Option.RemoteRequest;
using DHY.Core.Service;
using DHY.FrameWork.Core;
using DHY.FrameWork.Core.DataBase;
using DHY.FrameWork.Core.Logging;
using Furion;
using Furion.Logging.Extensions;
using Furion.SpecificationDocument;
using Furion.VirtualFileServer;
using IGeekFan.AspNetCore.Knife4jUI;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using OnceMi.AspNetCore.OSS;
using SixLabors.ImageSharp.Web.DependencyInjection;
using Yitter.IdGenerator;

namespace DHY.Web.Core;

public class Startup : AppStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 注册雪花Id
        var snowIdOpt = App.GetConfig<SnowIdOptions>("SnowId", true);
        YitIdHelper.SetIdGenerator(snowIdOpt);
        services.AddDhyDefaultSettings();
        // 配置选项
        services.AddProjectOptions();

        // 缓存注册
        services.AddCache();
        // SqlSugar
        services.AddSqlSugar(new SqlSugarCache(), DataBaseFilter.ConnectionFilter);
        // JWT
        services.AddJwt<JwtHandler>(enableGlobalAuthorize: true)
            // 添加 Signature 身份验证
            .AddSignatureAuthentication(options =>
            {
                options.Events = SysOpenAccessService.GetSignatureAuthenticationEventImpl();
            });
        // 允许跨域
        services.AddCorsAccessor();
        // 远程请求
        services.AddRemoteRequest(options =>
        {
            var httpClients = App.GetConfig<HttpClientSettingOptions>("HttpClientSetting");
            if (httpClients == null)
            {
                return;
            }
            foreach (var client in httpClients.Clients)
            {
                options.AddHttpClient(client.ClientName, c =>
                {
                    c.BaseAddress = new Uri(client.BaseUrl);
                });
            }

        });
        // 任务队列
        services.AddTaskQueue();
        // 任务调度
        services.AddSchedule(options =>
        {
            options.AddPersistence<DbJobPersistence>(); // 添加作业持久化器
        });
        // 脱敏检测
        services.AddSensitiveDetection();

        // Json序列化设置
        static void SetNewtonsoftJsonSetting(JsonSerializerSettings setting)
        {
            setting.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            setting.DateTimeZoneHandling = DateTimeZoneHandling.Local;
            setting.DateFormatString = "yyyy-MM-dd HH:mm:ss"; // 时间格式化
            setting.ReferenceLoopHandling = ReferenceLoopHandling.Ignore; // 忽略循环引用
                                                                          // setting.ContractResolver = new CamelCasePropertyNamesContractResolver(); // 解决动态对象属性名大写
                                                                          // setting.NullValueHandling = NullValueHandling.Ignore; // 忽略空值
                                                                          // setting.Converters.AddLongTypeConverters(); // long转string（防止js精度溢出） 超过17位开启
            setting.MetadataPropertyHandling = MetadataPropertyHandling.Ignore; // 解决DateTimeOffset异常
            setting.DateParseHandling = DateParseHandling.None; // 解决DateTimeOffset异常
                                                                //setting.Converters.Add(new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }); // 解决DateTimeOffset异常
        };

        services.AddControllersWithViews()
            .AddAppLocalization()
            .AddNewtonsoftJson(options => SetNewtonsoftJsonSetting(options.SerializerSettings))
            //.AddXmlSerializerFormatters()
            //.AddXmlDataContractSerializerFormatters()
            .AddInjectWithUnifyResult<AdminResultProvider>();

        // 三方授权登录OAuth
        services.AddOAuth();

        // ElasticSearch
        services.AddElasticSearch();

        // 配置Nginx转发获取客户端真实IP
        // 注1：如果负载均衡不是在本机通过 Loopback 地址转发请求的，一定要加上options.KnownNetworks.Clear()和options.KnownProxies.Clear()
        // 注2：如果设置环境变量 ASPNETCORE_FORWARDEDHEADERS_ENABLED 为 True，则不需要下面的配置代码
        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.All;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });

        // 限流服务
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        // 事件总线
        services.AddEventBus(options =>
        {
            options.UseUtcTimestamp = false;
            // 不启用事件日志
            options.LogEnabled = false;
            // 事件执行器（失败重试）
            options.AddExecutor<RetryEventHandlerExecutor>();
            options.UnobservedTaskExceptionHandler = (obj, args) =>
            {
                args.Exception.Message.LogError(obj?.ToJson());
            };
            #region Redis消息队列

            //// 替换事件源存储器
            //options.ReplaceStorer(serviceProvider =>
            //{
            //    var redisCache = serviceProvider.GetService<ICache>();
            //    // 创建默认内存通道事件源对象，可自定义队列路由key，如：dhy
            //    return new RedisEventSourceStorer(redisCache, "dhy", 3000);
            //});

            #endregion Redis消息队列

            #region RabbitMQ消息队列

            // 创建默认内存通道事件源对象，可自定义队列路由key，如：dhy
            //var eventBusOpt = App.GetConfig<EventBusOptions>("EventBus", true);
            //var rbmqEventSourceStorer = new RabbitMQEventSourceStore(new ConnectionFactory
            //{
            //    UserName = eventBusOpt.RabbitMQ.UserName,
            //    Password = eventBusOpt.RabbitMQ.Password,
            //    HostName = eventBusOpt.RabbitMQ.HostName,
            //    VirtualHost = eventBusOpt.RabbitMQ.VirtualHost,
            //    Port = eventBusOpt.RabbitMQ.Port
            //}, "dhyWeb", 3000, eventBusOpt.RabbitMQ.CommunicationGroup);

            //// 替换默认事件总线存储器
            //options.ReplaceStorerOrFallback(serviceProvider =>
            //{
            //    return rbmqEventSourceStorer;
            //});

            #endregion RabbitMQ消息队列
        });

        services.AddRpcEventBus();
        // 图像处理
        services.AddImageSharp();

        // OSS对象存储
        var ossOpt = App.GetConfig<OSSProviderOptions>("OSSProvider", true);
        services.AddOSSService(Enum.GetName(ossOpt.Provider), "OSSProvider");

        // 模板引擎
        services.AddViewEngine();

        // 即时通讯
        services.AddSignalR(SetNewtonsoftJsonSetting);
        //services.AddSingleton<IUserIdProvider, UserIdProvider>();

        // 系统日志
        services.AddLoggingSetup();
        services.AddDataBaseLogging();
        // 验证码
        services.AddCaptcha();

        //mqtt 通讯
        //services.AddMqtt();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        });

        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("DHY", "DhyBackend");
            await next();
        });

        // 图像处理
        app.UseImageSharp();

        // 特定文件类型（文件后缀）处理
        var contentTypeProvider = FS.GetFileExtensionContentTypeProvider();
        // contentTypeProvider.Mappings[".文件后缀"] = "MIME 类型";
        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypeProvider
        });

        //// 启用HTTPS
        //app.UseHttpsRedirection();

        // 启用OAuth
        app.UseOAuth();

        // 添加状态码拦截中间件
        app.UseUnifyResultStatusCodes();

        // 启用多语言，必须在 UseRouting 之前
        app.UseAppLocalization();

        // 路由注册
        app.UseRouting();

        // 启用跨域，必须在 UseRouting 和 UseAuthentication 之间注册
        app.UseCorsAccessor();

        // 启用鉴权授权
        app.UseAuthentication();
        app.UseAuthorization();

        // 限流组件（在跨域之后）
        app.UseIpRateLimiting();
        //app.UseClientRateLimiting();

        // 任务调度看板
        app.UseScheduleUI();

        // 配置Swagger-Knife4UI（路由前缀一致代表独立，不同则代表共存）
        app.UseKnife4UI(options =>
        {
            options.RoutePrefix = "kapi";
            foreach (var groupInfo in SpecificationDocumentBuilder.GetOpenApiGroups())
            {
                options.SwaggerEndpoint("/" + groupInfo.RouteTemplate, groupInfo.Title);
            }
        });

        app.UseInject(string.Empty);

        app.UseEndpoints(endpoints =>
        {
            // 注册集线器
            endpoints.MapHubs();

            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });

        //app.UseMqttSubscribers(options => options.DefaultResponse = new DhyMqttDefaultResult());
    }
}