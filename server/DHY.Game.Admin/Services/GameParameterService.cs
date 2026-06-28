using DHY.Game.Admin.Dtos;
using DHY.Game.AI.Options;
using DHY.Game.Core.Options;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DHY.Game.Admin.Services;

/// <summary>
/// 游戏参数管理服务
/// </summary>
[ApiDescriptionSettings("GameAdmin")]
public class GameParameterService : IDynamicApiController, ITransient
{
    private readonly IOptionsMonitor<GameOptions> _gameOptions;
    private readonly IOptionsMonitor<GameAiOptions> _aiOptions;

    public GameParameterService(
        IOptionsMonitor<GameOptions> gameOptions,
        IOptionsMonitor<GameAiOptions> aiOptions)
    {
        _gameOptions = gameOptions;
        _aiOptions = aiOptions;
    }

    /// <summary>
    /// 获取当前GameOptions全部配置
    /// </summary>
    [DisplayName("获取游戏参数配置")]
    [ApiDescriptionSettings(Name = "GetGameOptions"), HttpGet]
    public GameOptions GetGameOptions()
    {
        return _gameOptions.CurrentValue;
    }

    /// <summary>
    /// 运行时更新游戏参数
    /// </summary>
    [DisplayName("更新游戏参数配置")]
    [ApiDescriptionSettings(Name = "UpdateGameOptions"), HttpPost]
    public async Task UpdateGameOptionsAsync(UpdateGameOptionsInput input)
    {
        var configPath = GetConfigPath("GameCoreOptions.json");
        var jsonContent = await File.ReadAllTextAsync(configPath);

        var optionsNode = JObject.Parse(jsonContent);
        var gameNode = optionsNode["GameOptions"];

        if (gameNode == null)
            throw Oops.Oh("配置文件格式错误");

        // 按需更新字段
        if (input.MaxBaseHp.HasValue)
            gameNode["MaxBaseHp"] = input.MaxBaseHp.Value;
        if (input.HpPerConModifier.HasValue)
            gameNode["HpPerConModifier"] = input.HpPerConModifier.Value;
        if (input.TimeSegmentsPerDay.HasValue)
            gameNode["TimeSegmentsPerDay"] = input.TimeSegmentsPerDay.Value;
        if (input.OvertimePenalty.HasValue)
            gameNode["OvertimePenalty"] = input.OvertimePenalty.Value;
        if (input.WoundThresholdPercent.HasValue)
            gameNode["WoundThresholdPercent"] = input.WoundThresholdPercent.Value;
        if (input.RepositionInterval.HasValue)
            gameNode["RepositionInterval"] = input.RepositionInterval.Value;
        if (input.MaxExpertiseSlots.HasValue)
            gameNode["MaxExpertiseSlots"] = input.MaxExpertiseSlots.Value;
        if (input.MaxDungeonLevel.HasValue)
            gameNode["MaxDungeonLevel"] = input.MaxDungeonLevel.Value;

        var updatedJson = optionsNode.ToString(Formatting.Indented);

        await File.WriteAllTextAsync(configPath, updatedJson);
    }

    /// <summary>
    /// 获取AI配置(隐藏ApiKey)
    /// </summary>
    [DisplayName("获取AI配置")]
    [ApiDescriptionSettings(Name = "GetGameAiOptions"), HttpGet]
    public GameAiOptionsOutput GetGameAiOptions()
    {
        var options = _aiOptions.CurrentValue;

        var modelsOutput = new Dictionary<string, ModelConfigOutput>();
        if (options.Models != null)
        {
            foreach (var kv in options.Models)
            {
                modelsOutput[kv.Key] = new ModelConfigOutput
                {
                    AiRole = kv.Key,
                    ModelId = kv.Value.ModelId,
                    Temperature = kv.Value.Temperature,
                    EnableThinking = kv.Value.EnableThinking,
                    BaseUrl = kv.Value.BaseUrl,
                    ApiKeyMasked = MaskApiKey(kv.Value.ApiKey)
                };
            }
        }

        return new GameAiOptionsOutput
        {
            Models = modelsOutput,
            TimeoutSeconds = options.TimeoutSeconds,
            MaxRetries = options.MaxRetries
        };
    }

    /// <summary>
    /// 重置为默认配置
    /// </summary>
    [DisplayName("重置为默认配置")]
    [ApiDescriptionSettings(Name = "ResetToDefault"), HttpPost]
    public async Task ResetToDefaultAsync()
    {
        // 重置 GameCoreOptions
        var defaultGameOptions = new
        {
            GameOptions = new
            {
                MaxBaseHp = 30,
                HpPerConModifier = 3,
                TimeSegmentsPerDay = 4,
                OvertimePenalty = -2,
                WoundThresholdPercent = 25,
                RepositionInterval = 5,
                MaxExpertiseSlots = 10,
                MaxDungeonLevel = 4,
                ScoringWeights = new
                {
                    MainQuest = 40,
                    Execution = 25,
                    Exploration = 15,
                    Survival = 10,
                    WorldImpact = 10
                }
            }
        };

        var configPath = GetConfigPath("GameCoreOptions.json");
        var json = JsonConvert.SerializeObject(defaultGameOptions, Formatting.Indented);
        await File.WriteAllTextAsync(configPath, json);
    }

    /// <summary>
    /// 获取参数修改历史(通过审计字段)
    /// </summary>
    [DisplayName("获取参数修改历史")]
    [ApiDescriptionSettings(Name = "GetParameterHistory"), HttpGet]
    public async Task<object> GetParameterHistoryAsync()
    {
        // 返回配置文件的最后修改信息
        var gameCoreConfigPath = GetConfigPath("GameCoreOptions.json");
        var aiConfigPath = GetConfigPath("GameAiOptions.json");

        var result = new List<object>();

        if (File.Exists(gameCoreConfigPath))
        {
            var fileInfo = new FileInfo(gameCoreConfigPath);
            result.Add(new
            {
                ConfigFile = "GameCoreOptions.json",
                LastModified = fileInfo.LastWriteTime,
                Size = fileInfo.Length
            });
        }

        if (File.Exists(aiConfigPath))
        {
            var fileInfo = new FileInfo(aiConfigPath);
            result.Add(new
            {
                ConfigFile = "GameAiOptions.json",
                LastModified = fileInfo.LastWriteTime,
                Size = fileInfo.Length
            });
        }

        return await Task.FromResult(result);
    }

    #region 辅助方法

    private static string GetConfigPath(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Configuration", fileName);
        if (!File.Exists(path))
            path = Path.Combine(Directory.GetCurrentDirectory(), "Configuration", fileName);
        return path;
    }

    private static string MaskApiKey(string? apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return "未配置";
        if (apiKey.Length <= 4)
            return "****";
        return apiKey[..4] + new string('*', apiKey.Length - 4);
    }

    #endregion
}
