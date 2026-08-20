using System.Collections.Concurrent;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.AI.Options;
using DHY.Game.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DHY.Game.AI.Services;

/// <summary>
/// 预计算的行动选项缓存
/// </summary>
public class PrecomputedActionCache
{
    /// <summary>行动文本</summary>
    public string ActionText { get; set; } = "";

    /// <summary>方向提示</summary>
    public string Hint { get; set; } = "";

    /// <summary>预计算结果（含NarrativeInput、DiceResult、StateChanges等）</summary>
    public GameActionResult? Result { get; set; }

    /// <summary>预生成的叙事文本（非章节档=完整正文；章节档=已预取的前 N 段前缀）</summary>
    public string NarrativeText { get; set; } = "";

    /// <summary>
    /// 章节档续写起始分镜索引：
    /// -1 = 非章节档（NarrativeText 为完整正文，点选后直接回放）；
    /// >=0 = 章节档（NarrativeText 为已预取前缀，点选后回放前缀并从该索引起实时续写）。
    /// </summary>
    public int NextBeatIndex { get; set; } = -1;

    /// <summary>创建时间（用于TTL过期）</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 该选项是否可行：
    /// false 仅当预计算明确判定为不可行短路（Result 非空但 NarrativeInput 为空，即拒绝文案）；
    /// 预计算失败（Result 为空）时保持 true，以便点选时回退常规流程。
    /// </summary>
    public bool IsFeasible { get; set; } = true;
}

/// <summary>
/// 单个会话的行动选项缓存
/// </summary>
public class SessionActionCache
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }

    /// <summary>选项列表（恰好2个）</summary>
    public List<PrecomputedActionCache> Options { get; set; } = new();

    /// <summary>预计算是否全部完成</summary>
    public bool IsReady { get; set; }
}

/// <summary>
/// 行动预计算服务 - 并行预计算导演AI建议的行动选项并缓存结果
/// </summary>
public class ActionPrecomputeService : ISingleton
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActionPrecomputeService> _logger;
    private readonly GameAiOptions _options;
    private readonly ConcurrentDictionary<long, SessionActionCache> _cache = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(1);

    public ActionPrecomputeService(
        IServiceScopeFactory scopeFactory,
        IOptions<GameAiOptions> options,
        ILogger<ActionPrecomputeService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 并行预计算2个行动选项的完整AI流程（DryRun模式，不持久化）
    /// </summary>
    public async Task PrecomputeAsync(long sessionId, List<SuggestedActionInfo> options)
    {
        if (options == null || options.Count < 2)
        {
            _logger.LogDebug("预计算跳过: 建议行动选项不足2个, SessionId={SessionId}", sessionId);
            return;
        }

        // 初始化缓存（标记IsReady=false）
        var sessionCache = new SessionActionCache
        {
            SessionId = sessionId,
            IsReady = false,
            Options = options.Take(2).Select(o => new PrecomputedActionCache
            {
                ActionText = o.ActionText,
                Hint = o.Hint,
                CreatedAt = DateTime.Now
            }).ToList()
        };
        _cache[sessionId] = sessionCache;

        // 顺序执行2个选项的预计算（避免SqlSugar MARS并发冲突）
        var results = new (GameActionResult? result, string narrativeText, int nextBeatIndex)[sessionCache.Options.Count];
        for (int i = 0; i < sessionCache.Options.Count; i++)
        {
            results[i] = await PrecomputeSingleOptionAsync(sessionId, sessionCache.Options[i].ActionText, i);
        }

        try
        {
            // 检查缓存在计算期间是否被失效（玩家发起新行动触发InvalidateCache）
            if (!_cache.TryGetValue(sessionId, out var current) || current != sessionCache)
            {
                _logger.LogInformation("预计算结果已失效(缓存被清除), 丢弃: SessionId={SessionId}", sessionId);
                return;
            }

            for (int i = 0; i < results.Length; i++)
            {
                var (result, narrativeText, nextBeatIndex) = results[i];
                sessionCache.Options[i].Result = result;
                sessionCache.Options[i].NarrativeText = narrativeText;
                sessionCache.Options[i].NextBeatIndex = nextBeatIndex;
                // 仅当明确命中不可行短路（有 Result 但无 NarrativeInput）时标记为不可行
                sessionCache.Options[i].IsFeasible = !(result != null && result.NarrativeInput == null);
            }

            sessionCache.IsReady = true;
            _logger.LogInformation("预计算完成: SessionId={SessionId}, 选项数={Count}", sessionId, sessionCache.Options.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预计算部分失败: SessionId={SessionId}", sessionId);
            // 即使部分失败也标记完成，已完成的选项仍可使用
            sessionCache.IsReady = true;
        }
    }

    /// <summary>
    /// 根据选项索引获取缓存结果
    /// </summary>
    public PrecomputedActionCache? GetCachedResult(long sessionId, int optionIndex)
    {
        if (!_cache.TryGetValue(sessionId, out var sessionCache))
            return null;

        if (optionIndex < 0 || optionIndex >= sessionCache.Options.Count)
            return null;

        var option = sessionCache.Options[optionIndex];

        // 检查TTL
        if (DateTime.Now - option.CreatedAt > Ttl)
        {
            _logger.LogDebug("预计算缓存过期: SessionId={SessionId}, Index={Index}", sessionId, optionIndex);
            return null;
        }

        // 预计算未完成或失败
        if (option.Result == null)
            return null;

        return option;
    }

    /// <summary>
    /// 获取缓存的选项列表（供前端显示）
    /// </summary>
    public SessionActionCache? GetCachedOptions(long sessionId)
    {
        return _cache.TryGetValue(sessionId, out var sessionCache) ? sessionCache : null;
    }

    /// <summary>
    /// 清除指定会话的缓存
    /// </summary>
    public void InvalidateCache(long sessionId)
    {
        _cache.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// 预计算单个选项（在独立scope中执行，DryRun模式）
    /// </summary>
    private async Task<(GameActionResult? result, string narrativeText, int nextBeatIndex)> PrecomputeSingleOptionAsync(
        long sessionId, string actionText, int optionIndex)
    {
        using var scope = _scopeFactory.CreateScope();
        var aiCoordinator = scope.ServiceProvider.GetRequiredService<AiCoordinatorService>();
        var narrativeAi = scope.ServiceProvider.GetRequiredService<NarrativeAiService>();

        try
        {
            _logger.LogDebug("预计算开始: SessionId={SessionId}, Option={Index}, Action={Action}",
                sessionId, optionIndex, actionText);

            // 1. 执行完整AI管线（DryRun=true，不持久化）
            var processInput = new ProcessActionInput
            {
                SessionId = sessionId,
                ActionText = actionText,
                DryRun = true
            };

            var result = await aiCoordinator.ProcessPlayerActionAsync(processInput);

            // 2. 生成叙事文本
            //    - 非章节档：一次性预生成完整正文（点选秒开回放），nextBeatIndex=-1
            //    - 章节档：仅预取前 N 段（N=ChapterPrefetchBeats，默认1），其余分镜点选后边读边实时续写
            var narrativeText = "";
            var nextBeatIndex = -1;
            if (result?.NarrativeInput != null)
            {
                if (NarrativeAiService.IsChapterScale(result.NarrativeInput))
                {
                    var prefetch = _options.ChapterPrefetchBeats > 0 ? _options.ChapterPrefetchBeats : 1;
                    (narrativeText, nextBeatIndex) = await narrativeAi.GenerateChapterPrefixAsync(result.NarrativeInput, prefetch, sessionId);
                }
                else
                {
                    narrativeText = await narrativeAi.GenerateNarrativeAsync(result.NarrativeInput, sessionId);
                }
            }

            _logger.LogDebug("预计算完成: SessionId={SessionId}, Option={Index}, 叙事长度={Length}, 续写起始={NextBeatIndex}",
                sessionId, optionIndex, narrativeText.Length, nextBeatIndex);

            return (result, narrativeText, nextBeatIndex);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预计算失败: SessionId={SessionId}, Option={Index}", sessionId, optionIndex);
            return (null, "", -1);
        }
    }
}
