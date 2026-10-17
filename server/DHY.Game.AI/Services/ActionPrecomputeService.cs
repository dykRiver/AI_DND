using System.Collections.Concurrent;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Models;
using DHY.Game.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    /// <summary>
    /// 选项粒度（ActionScales.Detail / Advance）：预计算时已传给导演，
    /// 缓存未命中回退常规流程时也需带上，保证粗粒度选项两条路径都走章节档。
    /// </summary>
    public string Scale { get; set; } = ActionScales.Detail;

    /// <summary>预计算结果（含NarrativeInput、DiceResult、StateChanges等）</summary>
    public GameActionResult? Result { get; set; }

    /// <summary>【D4·完成快照】VIP流式预生成完成后的整章文本快照，仅用于日志与兜底；点选接管优先走 StreamBuffer。非VIP恒为空。</summary>
    public string NarrativeText { get; set; } = "";

    /// <summary>【路径B】VIP流式预生成的可重放缓冲：导演层就绪后即挂载，玩家点选时接管（回放已缓冲 + 续读同一流），全程一次AI请求。非VIP恒为 null。</summary>
    public NarrativeStreamBuffer? StreamBuffer { get; set; }

    /// <summary>【路径B·D1】该选项预生成流的取消令牌源：点选时取消未选选项以止损白烧；随缓存失效或生产结束释放。</summary>
    public CancellationTokenSource? NarrativeCts { get; set; }

    /// <summary>创建时间（用于TTL过期）</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 该选项是否可行：
    /// false 仅当预计算明确命中不可行短路（分类AI判 infeasible，即 Result.IsInfeasibleShortCircuit）；
    /// 导演解析失败/流程中断、或预计算失败（Result 为空）均保持 true，以便点选时回退常规流程。
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

    /// <summary>预计算是否全部完成（指导演层就绪；VIP叙事预生成在后台续跑，不影响此标志与本服务返回 Task 的完成）</summary>
    public bool IsReady { get; set; }

    /// <summary>选项推送给前端的时刻（埋点①：统计玩家从看到选项到点击的思考间隔）</summary>
    public DateTime OptionsShownAt { get; set; }
}

/// <summary>
/// 行动预计算服务 - 并行预计算导演AI建议的行动选项并缓存结果
/// </summary>
public class ActionPrecomputeService : ISingleton
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActionPrecomputeService> _logger;
    private readonly ConcurrentDictionary<long, SessionActionCache> _cache = new();
    private static readonly TimeSpan Ttl = TimeSpan.FromDays(1);

    public ActionPrecomputeService(
        IServiceScopeFactory scopeFactory,
        ILogger<ActionPrecomputeService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// 并行预计算2个行动选项的完整AI流程（DryRun模式，不持久化）
    /// </summary>
    /// <param name="isAdult">会话级成人模式：透传给预掷的 ProcessActionInput.IsAdultMode，使预计算按当前模式走成人/非成人全链路（对齐 worldDifficultyOverride 的“下一轮预计算起生效”语义）</param>
    /// <param name="isVip">会话级VIP模式：开启后导演层就绪即返回（按钮~15s放开，与非VIP一致），另起后台任务为两个选项各预生成一次叙事并回填 NarrativeText；回填后点选秒回放，回填前点选降级实时流式（代价：2个选项各生成一次，玩家只用1个，另1个叙事token浪费）</param>
    public async Task PrecomputeAsync(long sessionId, List<SuggestedActionInfo> options, int? worldDifficultyOverride = null, bool isAdult = false, bool isVip = false)
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
                Scale = o.Scale,
                CreatedAt = DateTime.Now
            }).ToList()
        };
        _cache[sessionId] = sessionCache;

        // ===== 阶段1：并行预计算导演层（两个选项）=====
        // 关键：ExecutionContext.SuppressFlow() 切断子任务对父 ExecutionContext 的继承，
        // 使每个选项子任务以空 AsyncLocal 上下文启动 → SqlSugarScope 为其分配独立连接，
        // 实现“任务间连接隔离(避免SqlSugar MARS并发冲突)、任务内共享同一连接(事务一致)”。
        var directorTasks = new Task<GameActionResult?>[sessionCache.Options.Count];
        for (int i = 0; i < sessionCache.Options.Count; i++)
        {
            int idx = i; // 闭包副本，避免捕获循环变量
            var optionActionText = sessionCache.Options[i].ActionText;
            var optionScale = sessionCache.Options[i].Scale;
            using (ExecutionContext.SuppressFlow())
            {
                directorTasks[idx] = Task.Run(() => PrecomputeDirectorAsync(sessionId, optionActionText, idx, optionScale, worldDifficultyOverride, isAdult));
            }
        }

        try
        {
            var results = await Task.WhenAll(directorTasks);

            // 检查缓存在计算期间是否被失效（玩家发起新行动触发InvalidateCache）
            if (!_cache.TryGetValue(sessionId, out var current) || current != sessionCache)
            {
                _logger.LogInformation("预计算结果已失效(缓存被清除), 丢弃: SessionId={SessionId}", sessionId);
                return;
            }

            for (int i = 0; i < results.Length; i++)
            {
                var result = results[i];
                sessionCache.Options[i].Result = result;
                // 仅当分类AI明确短路拒绝（infeasible）时标记不可行；
                // 导演解析失败/流程中断（Result 非空、NarrativeInput 为空但 Feasibility 非 infeasible）不属于"不可行"，
                // 保持可点击，点选时由 GetCachedResult 视为未命中回退常规流程
                sessionCache.Options[i].IsFeasible = !(result != null && result.IsInfeasibleShortCircuit);
            }

            // 导演层就绪即置 IsReady=true：本方法返回的 Task 到此完成，Hub 据此在 ~15s（导演层）放开按钮点击，
            // VIP 与非 VIP 一致，不再等叙事预生成（叙事在下方阶段2后台续跑）。
            sessionCache.IsReady = true;
            _logger.LogInformation("预计算完成(导演层): SessionId={SessionId}, 选项数={Count}", sessionId, sessionCache.Options.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预计算部分失败: SessionId={SessionId}", sessionId);
            // 即使部分失败也标记完成，已完成的选项仍可使用
            sessionCache.IsReady = true;
            // 导演层整体异常时不启动叙事预生成（无有效 NarrativeInput）
            return;
        }

        // ===== 阶段2：VIP 流式叙事预生成（后台续跑，不阻塞按钮放开）=====
        // 【路径B】仅 VIP 且导演层成功时启动：为 NarrativeInput 非空的选项各自创建一个可重放 buffer + CTS，
        //   并 detach 一个后台任务，用流式生成逐 chunk 写入 buffer（这是该选项唯一一次 AI 请求）。
        //   buffer 在生成开始前即挂到 Options[idx].StreamBuffer，因此玩家在任意时刻点选都能接管：
        //   已缓冲 chunk 立即回放、追平后续读同一个流，全程不新开请求。
        //   点选时通过 InvalidateCache(keepOptionIndexForNarrative) 取消未选选项的 CTS 止损（D1）。
        if (isVip)
        {
            for (int i = 0; i < sessionCache.Options.Count; i++)
            {
                int idx = i;
                var narrativeInput = sessionCache.Options[idx].Result?.NarrativeInput;
                // 不可行短路 / 导演解析失败（NarrativeInput=null）不预生成，点选时回放协调器已产出的文案或降级
                if (narrativeInput == null)
                    continue;

                var buffer = new NarrativeStreamBuffer();
                var cts = new CancellationTokenSource();
                sessionCache.Options[idx].StreamBuffer = buffer;
                sessionCache.Options[idx].NarrativeCts = cts;

                using (ExecutionContext.SuppressFlow())
                {
                    _ = Task.Run(() => PregenerateNarrativeAsync(sessionId, narrativeInput, idx, buffer, cts));
                }
            }
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

        // 导演解析失败/流程中断的兜底结果（Result 非空但 NarrativeInput 为空，且非不可行短路）：
        // 缓存中只有"世界没有反应"兜底文案，回放它会让玩家看到无意义叙事，视为未命中回退常规流程重跑
        if (option.Result.NarrativeInput == null && !option.Result.IsInfeasibleShortCircuit)
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
    /// 清除指定会话的缓存，并止损其 VIP 流式预生成（取消仍在跑的生产者）。
    /// keepOptionIndexForNarrative：点选命中路径传入被选选项索引，保住它的生产者继续跑完供接管（D1）；
    /// 其余调用点不传，即取消该会话所有预生成生产者。
    /// </summary>
    public void InvalidateCache(long sessionId, int? keepOptionIndexForNarrative = null)
    {
        if (_cache.TryGetValue(sessionId, out var sessionCache))
        {
            for (int i = 0; i < sessionCache.Options.Count; i++)
            {
                if (i == keepOptionIndexForNarrative) continue;   // 保住被选选项的生产者，供点选接管
                try { sessionCache.Options[i].NarrativeCts?.Cancel(); }
                catch (ObjectDisposedException) { /* 生产者已自行释放，忽略 */ }
            }
        }
        _cache.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// 标记选项已推送给前端的时刻（埋点①）。缓存不存在时静默忽略。
    /// </summary>
    public void MarkOptionsShown(long sessionId)
    {
        if (_cache.TryGetValue(sessionId, out var sessionCache))
            sessionCache.OptionsShownAt = DateTime.Now;
    }

    /// <summary>
    /// 读取选项推送时刻（埋点①：供点选入口计算玩家思考间隔）。无缓存或未标记返回 null。
    /// </summary>
    public DateTime? GetOptionsShownAt(long sessionId)
    {
        return _cache.TryGetValue(sessionId, out var sessionCache) && sessionCache.OptionsShownAt != default
            ? sessionCache.OptionsShownAt
            : null;
    }

    /// <summary>
    /// 预计算单个选项的导演层（在独立scope中执行，DryRun模式，不持久化）。
    /// 【L1改造】预计算深度砍到“导演层”：不生成叙事、不 await 书记官。
    ///   - 叙事：非VIP改由玩家点选后实时流式生成（Hub 的 StreamNarrativeLiveAsync）；VIP 由阶段2 PregenerateNarrativeAsync 后台预生成。
    ///     就绪时刻锁在“导演(~15s)”，VIP 不再因叙事预生成而延后按钮可点时刻。
    ///   - 书记官：ProcessPlayerActionAsync 内已 fire-and-forget 启动（result.ScribeTask），此处保留句柄、不 await，
    ///     让其后台与“就绪判定”并行完成；点选时再 await 回填 ScribeOutput/ItemHints/SuggestedActions。
    ///     RunScribeTaskAsync 自建独立DI scope且自捕异常，句柄可安全跨请求存活到点选。
    /// </summary>
    private async Task<GameActionResult?> PrecomputeDirectorAsync(
        long sessionId, string actionText, int optionIndex, string actionScale, int? worldDifficultyOverride = null, bool isAdult = false)
    {
        using var scope = _scopeFactory.CreateScope();
        var aiCoordinator = scope.ServiceProvider.GetRequiredService<AiCoordinatorService>();

        try
        {
            _logger.LogDebug("预计算开始(导演层): SessionId={SessionId}, Option={Index}, Action={Action}",
                sessionId, optionIndex, actionText);

            // 执行完整AI管线（DryRun=true，不持久化）
            var processInput = new ProcessActionInput
            {
                SessionId = sessionId,
                ActionText = actionText,
                DryRun = true,
                // 手动世界难度覆盖值透传：预掷的缓存选项也需带上玩家设定的难度修正
                WorldDifficultyOverride = worldDifficultyOverride,
                // 会话级成人模式透传：预掷按当前模式走成人/非成人全链路，使缓存的导演蓝图与模式一致
                IsAdultMode = isAdult,
                // 粗粒度推进选项：预演时就要让导演走章节档，否则缓存命中回放的仍是 normal 档结果
                ActionScale = actionScale
            };

            var result = await aiCoordinator.ProcessPlayerActionAsync(processInput);

            _logger.LogDebug("预计算完成(导演层): SessionId={SessionId}, Option={Index}, 可行={Feasible}",
                sessionId, optionIndex, result?.NarrativeInput != null);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "预计算失败(导演层): SessionId={SessionId}, Option={Index}", sessionId, optionIndex);
            return null;
        }
    }

    /// <summary>
    /// 【路径B·VIP模式】为单个选项在导演层就绪后后台流式预生成叙事正文，逐 chunk 写入可重放 buffer。
    /// 这是该选项唯一一次 AI 请求；玩家点选时接管此流（回放已缓冲 + 续读后续），不再新开请求。
    /// 被取消（未选选项止损，D1）视为正常收尾；失败则 buffer.Fail，点选降级实时流式（D2）。
    /// </summary>
    private async Task PregenerateNarrativeAsync(
        long sessionId, NarrativeInput narrativeInput, int optionIndex,
        NarrativeStreamBuffer buffer, CancellationTokenSource cts)
    {
        using var scope = _scopeFactory.CreateScope();
        try
        {
            var narrativeAi = scope.ServiceProvider.GetRequiredService<NarrativeAiService>();
            await foreach (var chunk in narrativeAi.StreamNarrativeAsync(narrativeInput, sessionId, cts.Token))
                buffer.Write(chunk);
            buffer.Complete();

            // D4：完成快照回填 NarrativeText（缓存可能已移除，仅作日志/兜底，消费者用 buffer 不依赖它）
            var fullText = buffer.FullText;
            if (_cache.TryGetValue(sessionId, out var sc) && optionIndex < sc.Options.Count)
                sc.Options[optionIndex].NarrativeText = fullText;

            _logger.LogInformation("VIP流式预生成完成: SessionId={SessionId}, Option={Index}, 字数={Length}",
                sessionId, optionIndex, fullText.Length);
        }
        catch (OperationCanceledException)
        {
            // D1：未选选项被取消止损，属预期路径
            buffer.Complete();
            _logger.LogInformation("VIP流式预生成被取消(未选选项止损): SessionId={SessionId}, Option={Index}", sessionId, optionIndex);
        }
        catch (Exception ex)
        {
            buffer.Fail(ex);
            _logger.LogWarning(ex, "VIP流式预生成失败(点选时降级实时流式): SessionId={SessionId}, Option={Index}", sessionId, optionIndex);
        }
        finally
        {
            cts.Dispose();
        }
    }
}
