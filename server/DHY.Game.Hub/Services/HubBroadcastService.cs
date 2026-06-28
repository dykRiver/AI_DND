using System.Diagnostics;
using System.Text.RegularExpressions;
using DHY.Game.AI.Dtos;
using DHY.Game.AI.Services;
using DHY.Game.Hub.Dtos;
using DHY.Game.Hub.Hubs;
using DHY.Game.Hub.Options;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DHY.Game.Hub.Services;

/// <summary>
/// Hub广播服务 - 通过IHubContext向客户端推送消息
/// </summary>
public class HubBroadcastService : ITransient
{
    private readonly IHubContext<GameSessionHub, IGameSessionHub> _hubContext;
    private readonly GameSessionManager _sessionManager;
    private readonly GameSignalROptions _options;
    private readonly ILogger<HubBroadcastService> _logger;

    public HubBroadcastService(
        IHubContext<GameSessionHub, IGameSessionHub> hubContext,
        GameSessionManager sessionManager,
        IOptions<GameSignalROptions> options,
        ILogger<HubBroadcastService> logger)
    {
        _hubContext = hubContext;
        _sessionManager = sessionManager;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// 实时流式推送叙事文本（从AI模型流式生成直接推送给客户端）
    /// 仅推送叙事内容，过滤掉思考内容
    /// </summary>
    /// <returns>完整叙事文本（用于后续日志记录）</returns>
    public async Task<string> StreamNarrativeLiveAsync(
        long userId,
        NarrativeAiService narrativeAi,
        NarrativeInput narrativeInput,
        long? sessionId,
        string chunkType = "narrative")
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return "";

        var client = _hubContext.Clients.Client(connectionId);
        var fullText = new System.Text.StringBuilder();
        var sw = Stopwatch.StartNew();
        var isFirst = true;

        try
        {
            await foreach (var chunk in narrativeAi.StreamNarrativeAsync(narrativeInput, sessionId))
            {
                if (string.IsNullOrEmpty(chunk))
                    continue;

                fullText.Append(chunk);

                var narrativeChunk = new NarrativeChunkDto
                {
                    Text = chunk,
                    ChunkType = chunkType,
                    IsLast = false,
                    Timestamp = DateTime.Now
                };

                await client.ReceiveNarrative(narrativeChunk);

                // 首字符到达时记录延迟
                if (isFirst)
                {
                    isFirst = false;
                    if (_options.StreamChunkDelayMs > 0)
                        _logger.LogDebug("叙事首token延迟: {Delay}ms", sw.ElapsedMilliseconds);
                }
            }

            // 发送最后一块标记
            await client.ReceiveNarrative(new NarrativeChunkDto
            {
                Text = "",
                ChunkType = chunkType,
                IsLast = true,
                Timestamp = DateTime.Now
            });

            sw.Stop();
            _logger.LogDebug("叙事流式完成: {Length}字, 耗时{Elapsed}ms",
                fullText.Length, sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "叙事流式推送异常");
            // 确保客户端收到IsLast，恢复输入
            try
            {
                await client.ReceiveNarrative(new NarrativeChunkDto
                {
                    Text = "",
                    ChunkType = chunkType,
                    IsLast = true,
                    Timestamp = DateTime.Now
                });
            }
            catch { /* 推送IsLast失败则忽略 */ }
        }

        return fullText.ToString();
    }

    /// <summary>
    /// 流式推送叙事文本
    /// 按中文标点拆分chunks，逐块延迟推送
    /// </summary>
    public async Task StreamNarrativeAsync(long userId, string narrative, string chunkType = "narrative")
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        var chunks = SplitNarrativeToChunks(narrative);
        var client = _hubContext.Clients.Client(connectionId);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = new NarrativeChunkDto
            {
                Text = chunks[i],
                ChunkType = chunkType,
                IsLast = i == chunks.Count - 1,
                Timestamp = DateTime.Now
            };

            await client.ReceiveNarrative(chunk);

            // 非最后一块时延迟
            if (!chunk.IsLast && _options.StreamChunkDelayMs > 0)
            {
                await Task.Delay(_options.StreamChunkDelayMs);
            }
        }
    }

    /// <summary>
    /// 推送骰子判定结果
    /// </summary>
    public async Task SendDiceResultAsync(long userId, DiceResultDto result)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).ReceiveDiceResult(result);
    }

    /// <summary>
    /// 推送游戏状态更新
    /// </summary>
    public async Task UpdateGameStateAsync(long userId, GameStateDto state)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).UpdateGameState(state);
    }

    /// <summary>
    /// 推送时段转换
    /// </summary>
    public async Task SendTimeTransitionAsync(long userId, TimeTransitionDto transition)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).SendTimeTransition(transition);
    }

    /// <summary>
    /// 推送玩家选择点
    /// </summary>
    public async Task RequestChoiceAsync(long userId, PlayerChoiceDto choices)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).RequestPlayerChoice(choices);
    }

    /// <summary>
    /// 推送生成进度
    /// </summary>
    public async Task SendProgressAsync(long userId, string phase, int percent, string? message = null)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).DungeonGenerating(new GeneratingProgressDto
        {
            Phase = phase,
            ProgressPercent = percent,
            Message = message
        });
    }

    /// <summary>
    /// 推送会话结束
    /// </summary>
    public async Task EndSessionAsync(long userId, SessionEndDto result)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).SessionEnded(result);
    }

    /// <summary>
    /// 推送系统消息
    /// </summary>
    public async Task SendSystemMessageAsync(long userId, string type, string message)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).ReceiveSystemMessage(new SystemMessageDto
        {
            Type = type,
            Message = message,
            Timestamp = DateTime.Now
        });
    }

    /// <summary>
    /// 推送高风险行动确认请求
    /// </summary>
    public async Task RequestDangerousActionConfirmAsync(long userId, DangerousActionDto action)
    {
        var connectionId = _sessionManager.GetConnectionId(userId);
        if (connectionId == null) return;

        await _hubContext.Clients.Client(connectionId).RequestDangerousActionConfirm(action);
    }

    #region 私有方法

    /// <summary>
    /// 按中文标点拆分叙事文本为多块
    /// 拆分规则：。！？；…… 以及换行符
    /// </summary>
    private static List<string> SplitNarrativeToChunks(string narrative)
    {
        if (string.IsNullOrWhiteSpace(narrative))
            return new List<string> { "" };

        // 按中文/英文标点拆分，保留标点在前一块
        var pattern = @"(?<=[。！？；…\.\!\?\n])";
        var parts = Regex.Split(narrative, pattern)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        return parts.Count > 0 ? parts : new List<string> { narrative };
    }

    #endregion
}
