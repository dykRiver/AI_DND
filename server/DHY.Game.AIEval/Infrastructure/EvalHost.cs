using System.Runtime.CompilerServices;
using DHY.Core;
using DHY.Game.Core.Services;
using Yitter.IdGenerator;

namespace DHY.Game.AIEval.Infrastructure;

/// <summary>
/// 评测宿主：不依赖 Furion 运行时，手工装配被测 AI 服务。
/// 数据库使用本地 SQLite 空库（仅承载评测期间的 AI 调用日志写入与物资官角色查询，
/// 角色表恒为空 => 物资官落库自动跳过，评测不触碰任何真实游戏数据）。
/// </summary>
public sealed class EvalHost : IDisposable
{
    /// <summary>评测专用伪会话ID（不存在的会话，确保落库路径全部空转）</summary>
    public const long EvalSessionId = 9_000_000_001L;

    public GameAiOptions AiOptions { get; }
    public AiModelFactory ModelFactory { get; }
    public PromptTemplateService PromptService { get; }
    public ActionClassifierService Classifier { get; }
    public DirectorAiService Director { get; }
    public NarrativeAiService Narrative { get; }
    public QuartermasterAiService Quartermaster { get; }
    public ILoggerFactory LoggerFactory { get; }

    private readonly ServiceProvider _serviceProvider;
    private readonly SqlSugarScope _evalDb;

    private EvalHost(GameAiOptions aiOptions, ServiceProvider serviceProvider, SqlSugarScope evalDb)
    {
        AiOptions = aiOptions;
        _serviceProvider = serviceProvider;
        _evalDb = evalDb;

        LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        ModelFactory = serviceProvider.GetRequiredService<AiModelFactory>();
        PromptService = serviceProvider.GetRequiredService<PromptTemplateService>();
        Classifier = serviceProvider.GetRequiredService<ActionClassifierService>();
        Director = serviceProvider.GetRequiredService<DirectorAiService>();
        Narrative = serviceProvider.GetRequiredService<NarrativeAiService>();
        Quartermaster = serviceProvider.GetRequiredService<QuartermasterAiService>();
    }

    /// <summary>
    /// 构建评测宿主。configPath 为空时按候选顺序查找 GameAiOptions.json。
    /// </summary>
    public static EvalHost Build(string? configPath)
    {
        // 1. 定位配置文件
        var path = ResolveConfigPath(configPath);
        Console.WriteLine($"[配置] 使用 AI 配置: {path}");

        var configRoot = new ConfigurationBuilder()
            .AddJsonFile(path, optional: false)
            .Build();
        var aiOptions = configRoot.GetSection("GameAi").Get<GameAiOptions>()
            ?? throw new InvalidOperationException("配置文件中缺少 GameAi 节");

        // 评测期间默认关闭 AI 调试日志（控制台会被全量 prompt 日志淹没），报告本身就是产出物
        aiOptions.EnableDebugLog = false;

        // 2. SQLite 评测库（雪花Id生成器与线上一致，保证日志表主键不冲突）
        StaticConfig.CustomSnowFlakeFunc = YitIdHelper.NextId;
        var dbPath = Path.Combine(AppContext.BaseDirectory, "data");
        Directory.CreateDirectory(dbPath);
        var evalDb = new SqlSugarScope(new ConnectionConfig
        {
            DbType = DbType.Sqlite,
            ConnectionString = $"DataSource={Path.Combine(dbPath, "eval.db")}",
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
        evalDb.CodeFirst.InitTables(typeof(GameAiCallLog), typeof(GameCharacter));

        // 3. 手工装配服务
        var services = new ServiceCollection();
        services.AddLogging(b => b.AddSimpleConsole(o =>
        {
            o.SingleLine = true;
            o.TimestampFormat = "HH:mm:ss ";
        }).SetMinimumLevel(LogLevel.Warning));
        services.AddHttpClient();
        services.AddSingleton(Options.Create(aiOptions));
        services.AddSingleton(new PromptTemplateService());
        services.AddSingleton<AiModelFactory>();

        // 仓储：跳过构造器（构造器依赖 Furion 运行时），直接注入评测库上下文
        services.AddSingleton(CreateRepository<GameAiCallLog>(evalDb));
        services.AddSingleton(CreateRepository<GameCharacter>(evalDb));
        services.AddSingleton(CreateRepository<GameInventoryItem>(evalDb));
        services.AddSingleton(CreateRepository<GameKnownAsset>(evalDb));

        services.AddSingleton<InventoryService>();
        services.AddSingleton<KnownAssetService>();
        services.AddSingleton<ActionClassifierService>();
        services.AddSingleton<DirectorAiService>();
        services.AddSingleton<NarrativeAiService>();
        services.AddSingleton<QuartermasterAiService>();

        var provider = services.BuildServiceProvider();
        return new EvalHost(aiOptions, provider, evalDb);
    }

    /// <summary>
    /// 创建绑定评测库的仓储实例（绕过依赖 Furion 的构造器）
    /// </summary>
    private static SqlSugarRepository<T> CreateRepository<T>(SqlSugarScope db) where T : class, new()
    {
        var repo = (SqlSugarRepository<T>)RuntimeHelpers.GetUninitializedObject(typeof(SqlSugarRepository<T>));
        ((SimpleClient<T>)repo).Context = db;
        return repo;
    }

    /// <summary>
    /// 配置文件候选路径：显式指定 > 运行目录 GameAiOptions.json > 源码树 Web.Entry 配置
    /// </summary>
    private static string ResolveConfigPath(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var full = Path.GetFullPath(explicitPath);
            if (File.Exists(full)) return full;
            throw new FileNotFoundException($"指定的配置文件不存在: {full}");
        }

        var candidates = new List<string> { Path.Combine(AppContext.BaseDirectory, "GameAiOptions.json") };

        // 从运行目录向上查找源码树中的配置（最多向上 8 级）：
        // Web.Entry 运行副本 / Application 配置源（构建时被复制到各宿主 bin）
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
        {
            candidates.Add(Path.Combine(dir.FullName, "DHY.FrameWork.Web.Entry", "Configuration", "GameAiOptions.json"));
            candidates.Add(Path.Combine(dir.FullName, "DHY.FrameWork.Application", "Configuration", "GameAiOptions.json"));
            candidates.Add(Path.Combine(dir.FullName, "Configuration", "GameAiOptions.json"));
        }

        var found = candidates.FirstOrDefault(File.Exists);
        return found ?? throw new FileNotFoundException(
            "未找到 GameAiOptions.json。请用 --config 指定路径，或将该文件复制到评测运行目录。");
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _evalDb.Dispose();
    }
}
