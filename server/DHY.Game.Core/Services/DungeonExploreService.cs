using DHY.Game.Core.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DHY.Game.Core.Services;

/// <summary>
/// 副本探索服务（面向玩家）
/// </summary>
[ApiDescriptionSettings("Game")]
public class DungeonExploreService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameDungeonTemplate> _templateRep;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameNarrativeLog> _narrativeLogRep;

    public DungeonExploreService(
        SqlSugarRepository<GameDungeonTemplate> templateRep,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameNarrativeLog> narrativeLogRep)
    {
        _templateRep = templateRep;
        _sessionRep = sessionRep;
        _characterRep = characterRep;
        _narrativeLogRep = narrativeLogRep;
    }

    /// <summary>
    /// 获取所有可用副本模板列表（不含内部管理字段）
    /// </summary>
    [DisplayName("获取副本列表")]
    [HttpGet("getAllTemplates")]
    public async Task<List<DungeonTemplateOutput>> GetAllTemplatesAsync()
    {
        var templates = await _templateRep.AsQueryable()
            .Where(t => !t.IsDelete)
            .ToListAsync();

        return templates
            .OrderByDescending(t => t.Difficulty)
            .ThenByDescending(t => t.CreateTime)
            .Select(MapToOutput)
            .ToList();
    }

    /// <summary>
    /// 获取副本模板详情（不含内部管理字段）
    /// </summary>
    [DisplayName("获取副本详情")]
    [HttpGet("getTemplateDetail")]
    public async Task<DungeonTemplateOutput> GetTemplateDetailApiAsync([FromQuery] IdInput input)
    {
        return await GetTemplateDetailAsync(input.Id);
    }

    /// <summary>
    /// 获取副本详情内部实现
    /// </summary>
    internal async Task<DungeonTemplateOutput> GetTemplateDetailAsync(long id)
    {
        var template = await _templateRep.GetFirstAsync(t => t.Id == id && !t.IsDelete);
        if (template == null)
            throw Oops.Oh("副本不存在");

        return MapToOutput(template);
    }

    /// <summary>
    /// 实体映射为输出DTO（隐藏BasePrompt）
    /// </summary>
    private static DungeonTemplateOutput MapToOutput(GameDungeonTemplate t)
    {
        return new DungeonTemplateOutput
        {
            Id = t.Id,
            Name = t.Name,
            WorldTheme = t.WorldTheme,
            Difficulty = t.Difficulty,
            TimeLimitDays = t.TimeLimitDays,
            Tags = t.Tags ?? new List<string>(),
            Description = t.Description,
        };
    }

    /// <summary>
    /// 检查当前用户是否有进行中的副本会话（断线续玩）
    /// </summary>
    [DisplayName("检查活跃会话")]
    [HttpGet("checkActiveSession")]
    public async Task<ActiveSessionCheckOutput?> CheckActiveSessionApiAsync([FromQuery] UserIdInput input)
    {
        return await CheckActiveSessionAsync(input.UserId);
    }

    /// <summary>
    /// 检查活跃会话内部实现
    /// </summary>
    internal async Task<ActiveSessionCheckOutput?> CheckActiveSessionAsync(long userId)
    {
        // 1. 查找最新的进行中会话
        var session = await _sessionRep.AsQueryable()
            .Where(s => s.UserId == userId && (s.Status == 0 || s.Status == 4) && !s.IsDelete)
            .OrderByDescending(s => s.StartTime)
            .FirstAsync();

        if (session == null)
            return null;

        // 2. 查询副本模板
        var template = await _templateRep.GetFirstAsync(t => t.Id == session.TemplateId);
        var dungeonName = template?.Name ?? "未知副本";

        // 3. 查询角色
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == session.Id);

        // 4. 解析世界设定
        var worldBackground = "";
        var keyLocations = new List<string>();
        if (!string.IsNullOrEmpty(session.WorldSetting))
        {
            try
            {
                var ws = JObject.Parse(session.WorldSetting);
                var parts = new List<string>();
                if (ws["era"]?.ToString() is string era && !string.IsNullOrEmpty(era))
                    parts.Add($"时代: {era}");
                if (ws["technology_level"]?.ToString() is string tech && !string.IsNullOrEmpty(tech))
                    parts.Add($"科技水平: {tech}");
                if (ws["culture"]?.ToString() is string culture && !string.IsNullOrEmpty(culture))
                    parts.Add($"文化: {culture}");
                if (ws["geography"]?.ToString() is string geo && !string.IsNullOrEmpty(geo))
                    parts.Add($"地理: {geo}");
                worldBackground = string.Join("\n", parts);

                var locations = ws["key_locations"] as JArray;
                if (locations != null)
                {
                    foreach (var loc in locations)
                    {
                        var name = loc["name"]?.ToString() ?? "";
                        var desc = loc["description"]?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(name))
                            keyLocations.Add($"{name}: {desc}");
                    }
                }
            }
            catch { /* 解析失败则留空 */ }
        }

        // 5. 解析主线任务
        var mainQuestObjective = "";
        var mainQuestNodes = new List<string>();
        if (!string.IsNullOrEmpty(session.MainQuest))
        {
            try
            {
                var mq = JObject.Parse(session.MainQuest);
                mainQuestObjective = mq["objective"]?.ToString() ?? "";
                var nodes = mq["key_nodes"] as JArray;
                if (nodes != null)
                    mainQuestNodes = nodes.Select(n => n.ToString()).ToList();
            }
            catch { /* 解析失败则留空 */ }
        }

        // 6. 构建游戏状态
        var hpPercent = character != null && character.MaxHp > 0
            ? (int)(character.CurrentHp * 100.0 / character.MaxHp) : 100;
        var status = hpPercent switch
        {
            > 75 => "正常",
            > 50 => "轻伤",
            > 25 => "重伤",
            _ => "濒死"
        };
        var segmentName = session.CurrentSegment switch
        {
            0 => "上午",
            1 => "下午",
            2 => "傍晚",
            3 => "夜间",
            _ => "未知"
        };

        // 7. 查询最近20条叙事日志
        var logs = await _narrativeLogRep.AsQueryable()
            .Where(l => l.SessionId == session.Id && !l.IsDelete)
            .OrderByDescending(l => l.InteractionIndex)
            .Take(20)
            .ToListAsync();

        var recentNarratives = logs
            .OrderBy(l => l.InteractionIndex)
            .Select(l => new ActiveSessionNarrative
            {
                Text = l.NarrativeText ?? "",
                ChunkType = l.PlayerInput == "[副本开始]" ? "scene_transition" : "narrative"
            })
            .ToList();

        return new ActiveSessionCheckOutput
        {
            SessionId = session.Id,
            TemplateId = session.TemplateId,
            DungeonName = dungeonName,
            WorldInfo = new ActiveSessionWorldInfo
            {
                DungeonName = dungeonName,
                WorldBackground = worldBackground,
                MainQuestObjective = mainQuestObjective,
                MainQuestNodes = mainQuestNodes,
                KeyLocations = keyLocations
            },
            GameState = new ActiveSessionGameState
            {
                CurrentHp = character?.CurrentHp ?? 100,
                MaxHp = character?.MaxHp ?? 100,
                HpPercent = hpPercent,
                Status = status,
                CurrentDay = session.CurrentDay,
                CurrentSegment = segmentName,
                TensionLevel = session.TensionLevel,
                IsFatigued = character?.IsFatigued ?? false,
                IsInCombat = character?.IsInCombat ?? false
            },
            RecentNarratives = recentNarratives
        };
    }
}
