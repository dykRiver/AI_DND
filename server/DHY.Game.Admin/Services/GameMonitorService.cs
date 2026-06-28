using DHY.Game.Admin.Dtos;
using DHY.Game.Core.Entities;

namespace DHY.Game.Admin.Services;

/// <summary>
/// 游戏监控服务
/// </summary>
[ApiDescriptionSettings("GameAdmin")]
public class GameMonitorService : IDynamicApiController, ITransient
{
    private readonly ISqlSugarClient _db;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameAiCallLog> _aiCallLogRep;

    public GameMonitorService(
        ISqlSugarClient db,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameAiCallLog> aiCallLogRep)
    {
        _db = db;
        _sessionRep = sessionRep;
        _aiCallLogRep = aiCallLogRep;
    }

    /// <summary>
    /// 获取当前活跃的副本会话列表
    /// </summary>
    [DisplayName("获取活跃会话列表")]
    [HttpGet("activeSessions")]
    public async Task<List<ActiveSessionOutput>> GetActiveSessions()
    {
        return await _sessionRep.AsQueryable()
            .Where(s => s.Status == 0 && !s.IsDelete)
            .LeftJoin<GameDungeonTemplate>((s, t) => s.TemplateId == t.Id)
            .OrderByDescending((s, t) => s.StartTime)
            .Select((s, t) => new ActiveSessionOutput
            {
                SessionId = s.Id,
                UserId = s.UserId,
                DungeonName = t.Name,
                StartTime = s.StartTime,
                InteractionCount = s.InteractionCount,
                Status = s.Status
            })
            .ToListAsync();
    }

    /// <summary>
    /// 获取会话详情
    /// </summary>
    [DisplayName("获取会话详情")]
    [HttpGet("sessionDetail")]
    public async Task<object> GetSessionDetailAsync([FromQuery] SessionDetailQueryInput input)
    {
        var session = await _sessionRep.AsQueryable()
            .Where(s => s.Id == input.SessionId)
            .FirstAsync();

        if (session == null)
            throw Oops.Oh("会话不存在");

        // 获取最近AI调用记录
        var recentCalls = await _aiCallLogRep.AsQueryable()
            .Where(c => c.SessionId == input.SessionId)
            .OrderByDescending(c => c.CreateTime)
            .Take(20)
            .ToListAsync();

        return new
        {
            Session = session,
            RecentAiCalls = recentCalls
        };
    }

    /// <summary>
    /// 日统计
    /// </summary>
    [DisplayName("获取日统计数据")]
    [HttpGet("dailyStats")]
    public async Task<DailyStatsOutput> GetDailyStatsAsync([FromQuery] DailyStatsQueryInput input)
    {
        var dayStart = input.Date.Date;
        var dayEnd = dayStart.AddDays(1);

        // 新建会话数
        var newSessions = await _sessionRep.AsQueryable()
            .Where(s => s.StartTime >= dayStart && s.StartTime < dayEnd && !s.IsDelete)
            .CountAsync();

        // 已完成会话数
        var completedSessions = await _sessionRep.AsQueryable()
            .Where(s => s.EndTime >= dayStart && s.EndTime < dayEnd && s.Status == 1 && !s.IsDelete)
            .CountAsync();

        // 已放弃会话数
        var abandonedSessions = await _sessionRep.AsQueryable()
            .Where(s => s.EndTime >= dayStart && s.EndTime < dayEnd && s.Status == 2 && !s.IsDelete)
            .CountAsync();

        // 平均时长(分钟) - 当天已结束的会话
        var completedSessionsData = await _sessionRep.AsQueryable()
            .Where(s => s.EndTime >= dayStart && s.EndTime < dayEnd && s.EndTime != null && !s.IsDelete)
            .ToListAsync();

        double avgDuration = 0;
        double avgInteractions = 0;

        if (completedSessionsData.Any())
        {
            avgDuration = completedSessionsData
                .Where(s => s.EndTime.HasValue)
                .Average(s => (s.EndTime!.Value - s.StartTime).TotalMinutes);
            avgInteractions = completedSessionsData.Average(s => s.InteractionCount);
        }

        return new DailyStatsOutput
        {
            NewSessions = newSessions,
            CompletedSessions = completedSessions,
            AbandonedSessions = abandonedSessions,
            AvgDurationMinutes = Math.Round(avgDuration, 1),
            AvgInteractions = Math.Round(avgInteractions, 1)
        };
    }

    /// <summary>
    /// 总览数据
    /// </summary>
    [DisplayName("获取总览数据")]
    [HttpGet("overview")]
    public async Task<OverviewOutput> GetOverviewAsync()
    {
        // 总用户数(去重)
        var totalUsers = await _sessionRep.AsQueryable()
            .Where(s => !s.IsDelete)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync();

        // 总会话数
        var totalSessions = await _sessionRep.AsQueryable()
            .Where(s => !s.IsDelete)
            .CountAsync();

        // 活跃会话数
        var activeSessions = await _sessionRep.AsQueryable()
            .Where(s => s.Status == 0 && !s.IsDelete)
            .CountAsync();

        // 总AI调用次数
        var totalAiCalls = await _aiCallLogRep.AsQueryable()
            .Where(c => !c.IsDelete)
            .CountAsync();

        // 总Token消耗
        var totalTokens = await _aiCallLogRep.AsQueryable()
            .Where(c => !c.IsDelete)
            .SumAsync(c => c.TotalTokens);

        return new OverviewOutput
        {
            TotalUsers = totalUsers,
            TotalSessions = totalSessions,
            ActiveSessions = activeSessions,
            TotalAiCalls = totalAiCalls,
            TotalTokens = totalTokens
        };
    }
}
