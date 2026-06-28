namespace DHY.Game.Core.Services;

/// <summary>
/// 时段系统服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class TimeSegmentService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameTimeSegment> _timeSegmentRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly ISqlSugarClient _db;
    private readonly GameOptions _options;

    public TimeSegmentService(
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameTimeSegment> timeSegmentRep,
        SqlSugarRepository<GameCharacter> characterRep,
        ISqlSugarClient db,
        IOptions<GameOptions> options)
    {
        _sessionRep = sessionRep;
        _timeSegmentRep = timeSegmentRep;
        _characterRep = characterRep;
        _db = db;
        _options = options.Value;
    }

    /// <summary>
    /// 推进一个时段
    /// 上午(0)→下午(1)→傍晚(2)→夜间(3)→下一天上午(0)
    /// 夜间行动后 = 加班, OvertimeCount++
    /// </summary>
    [DisplayName("推进时段")]
    [HttpPost("advanceTime")]
    public async Task<TimeSegmentInfo> AdvanceTimeAsync([FromBody] SessionIdInput input)
    {
        var sessionId = input.SessionId;
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        var isOvertime = session.CurrentSegment >= 3; // 夜间行动后为加班

        // 记录当前时段
        var segmentRecord = new GameTimeSegment
        {
            SessionId = sessionId,
            Day = session.CurrentDay,
            Segment = session.CurrentSegment,
            IsOvertime = isOvertime
        };
        await _timeSegmentRep.AsInsertable(segmentRecord).ExecuteCommandAsync();

        // 推进时段
        if (session.CurrentSegment >= 3)
        {
            // 夜间→下一天上午
            session.CurrentDay++;
            session.CurrentSegment = 0;
            session.OvertimeCount++;

            // 设置角色疲劳
            var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
            if (character != null)
            {
                character.IsFatigued = true;
                await _characterRep.AsUpdateable(character)
                    .UpdateColumns(c => new { c.IsFatigued })
                    .ExecuteCommandAsync();
            }
        }
        else
        {
            session.CurrentSegment++;
        }

        await _sessionRep.AsUpdateable(session)
            .UpdateColumns(s => new { s.CurrentDay, s.CurrentSegment, s.OvertimeCount })
            .ExecuteCommandAsync();

        return new TimeSegmentInfo
        {
            Day = session.CurrentDay,
            Segment = session.CurrentSegment,
            SegmentName = GetSegmentName(session.CurrentSegment),
            IsOvertime = isOvertime,
            OvertimeCount = session.OvertimeCount
        };
    }

    /// <summary>
    /// 获取当前时段信息
    /// </summary>
    [DisplayName("获取当前时段")]
    [HttpGet("getCurrentTime")]
    public async Task<TimeSegmentInfo> GetCurrentTimeAsync([FromQuery] SessionIdInput input)
    {
        var session = await _sessionRep.GetFirstAsync(s => s.Id == input.SessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        return new TimeSegmentInfo
        {
            Day = session.CurrentDay,
            Segment = session.CurrentSegment,
            SegmentName = GetSegmentName(session.CurrentSegment),
            IsOvertime = false,
            OvertimeCount = session.OvertimeCount
        };
    }

    /// <summary>
    /// 长休息
    /// 恢复HP 50%(向上取整)，清除加班疲劳状态，消耗整个夜间时段
    /// </summary>
    [DisplayName("长休息")]
    [HttpPost("longRest")]
    public async Task<TimeSegmentInfo> LongRestAsync([FromBody] SessionIdInput input)
    {
        var sessionId = input.SessionId;
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        if (session.CurrentSegment != 3)
            throw Oops.Oh("只能在夜间时段进行长休息");

        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");

        try
        {
            _db.AsTenant().BeginTran();

            // HP恢复50%（向上取整）
            var healAmount = (int)Math.Ceiling(character.MaxHp * 0.5);
            character.CurrentHp = Math.Min(character.CurrentHp + healAmount, character.MaxHp);
            character.IsFatigued = false; // 清除疲劳

            await _db.Updateable(character)
                .UpdateColumns(c => new { c.CurrentHp, c.IsFatigued })
                .ExecuteCommandAsync();

            // 记录休息时段
            var segmentRecord = new GameTimeSegment
            {
                SessionId = sessionId,
                Day = session.CurrentDay,
                Segment = session.CurrentSegment,
                ActionSummary = "长休息",
                HpChange = healAmount,
                IsOvertime = false
            };
            await _db.Insertable(segmentRecord).ExecuteCommandAsync();

            // 推进到下一天
            session.CurrentDay++;
            session.CurrentSegment = 0;

            await _db.Updateable(session)
                .UpdateColumns(s => new { s.CurrentDay, s.CurrentSegment })
                .ExecuteCommandAsync();

            _db.AsTenant().CommitTran();
        }
        catch
        {
            _db.AsTenant().RollbackTran();
            throw;
        }

        return new TimeSegmentInfo
        {
            Day = session.CurrentDay,
            Segment = session.CurrentSegment,
            SegmentName = GetSegmentName(session.CurrentSegment),
            IsOvertime = false,
            OvertimeCount = session.OvertimeCount
        };
    }

    /// <summary>
    /// 判断当前是否加班
    /// </summary>
    [DisplayName("判断是否加班")]
    [HttpGet("isOvertime")]
    public async Task<bool> IsOvertimeAsync([FromQuery] SessionIdInput input)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
        return character?.IsFatigued ?? false;
    }

    /// <summary>
    /// 获取当日剩余时段数
    /// </summary>
    [DisplayName("获取剩余时段")]
    [HttpGet("getRemainingSegments")]
    public async Task<int> GetRemainingSegmentsAsync([FromQuery] SessionIdInput input)
    {
        var session = await _sessionRep.GetFirstAsync(s => s.Id == input.SessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        return _options.TimeSegmentsPerDay - session.CurrentSegment - 1;
    }

    /// <summary>
    /// 获取时段中文名
    /// </summary>
    private string GetSegmentName(int segment)
    {
        return segment switch
        {
            0 => "上午",
            1 => "下午",
            2 => "傍晚",
            3 => "夜间",
            _ => "未知"
        };
    }
}

/// <summary>
/// 时段信息
/// </summary>
public class TimeSegmentInfo
{
    /// <summary>天数</summary>
    public int Day { get; set; }
    /// <summary>时段 (0-3)</summary>
    public int Segment { get; set; }
    /// <summary>时段名称</summary>
    public string SegmentName { get; set; } = "";
    /// <summary>是否加班</summary>
    public bool IsOvertime { get; set; }
    /// <summary>累计加班次数</summary>
    public int OvertimeCount { get; set; }
}
