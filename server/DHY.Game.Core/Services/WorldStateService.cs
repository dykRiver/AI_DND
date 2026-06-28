using DHY.Game.Core.Dtos;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DHY.Game.Core.Services;

/// <summary>
/// 世界状态引擎服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class WorldStateService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameWorldState> _worldStateRep;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameNpcProfile> _npcRep;
    private readonly SqlSugarRepository<GameNarrativeLog> _narrativeRep;
    private readonly SqlSugarRepository<GameInventoryItem> _itemRep;
    private readonly SqlSugarRepository<GameBaseSkill> _baseSkillRep;
    private readonly ISqlSugarClient _db;
    private readonly GameOptions _options;

    public WorldStateService(
        SqlSugarRepository<GameWorldState> worldStateRep,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameNpcProfile> npcRep,
        SqlSugarRepository<GameNarrativeLog> narrativeRep,
        SqlSugarRepository<GameInventoryItem> itemRep,
        SqlSugarRepository<GameBaseSkill> baseSkillRep,
        ISqlSugarClient db,
        IOptions<GameOptions> options)
    {
        _worldStateRep = worldStateRep;
        _sessionRep = sessionRep;
        _characterRep = characterRep;
        _npcRep = npcRep;
        _narrativeRep = narrativeRep;
        _itemRep = itemRep;
        _baseSkillRep = baseSkillRep;
        _db = db;
        _options = options.Value;
    }

    /// <summary>
    /// 初始化世界状态
    /// </summary>
    [DisplayName("初始化世界状态")]
    [HttpPost("initializeWorldState")]
    public async Task<GameWorldState> InitializeWorldStateApiAsync([FromBody] InitializeWorldStateInput input)
        => await InitializeWorldStateAsync(input.SessionId, input.ArchitectOutput);

    /// <summary>
    /// 初始化世界状态（内部调用）
    /// </summary>
    internal async Task<GameWorldState> InitializeWorldStateAsync(long sessionId, string architectOutput)
    {
        var worldState = new GameWorldState
        {
            SessionId = sessionId,
            StateJson = architectOutput,
            SnapshotType = "current",
            InteractionIndex = 0
        };

        await _worldStateRep.AsInsertable(worldState).ExecuteCommandAsync();
        return worldState;
    }

    /// <summary>
    /// 获取当前世界状态JSON
    /// </summary>
    [DisplayName("获取当前世界状态")]
    [HttpGet("getCurrentState")]
    public async Task<GameWorldState> GetCurrentStateApiAsync([FromQuery] SessionIdInput input)
        => await GetCurrentStateAsync(input.SessionId);

    /// <summary>
    /// 获取当前世界状态（内部调用）
    /// </summary>
    internal async Task<GameWorldState> GetCurrentStateAsync(long sessionId)
    {
        var state = await _worldStateRep.AsQueryable()
            .Where(s => s.SessionId == sessionId && s.SnapshotType == "current")
            .OrderByDescending(s => s.InteractionIndex)
            .FirstAsync();

        if (state == null)
            throw Oops.Oh("世界状态未初始化");

        return state;
    }

    /// <summary>
    /// 应用状态变更（结构化，由导演AI输出WorldStateChangesDto）
    /// </summary>
    [DisplayName("应用状态变更")]
    [HttpPost("applyChanges")]
    public async Task<GameWorldState> ApplyChangesApiAsync([FromBody] ApplyStateChangesInput input)
        => await ApplyChangesAsync(input.SessionId, input.Changes, input.InteractionCount);

    /// <summary>
    /// 应用状态变更（内部调用）
    /// 将WorldStateChangesDto合并到当前局面快照，追加change_history
    /// </summary>
    internal async Task<GameWorldState> ApplyChangesAsync(long sessionId, WorldStateChangesDto changes, int interactionCount)
    {
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        // 获取当前状态
        var currentState = await _worldStateRep.AsQueryable()
            .Where(s => s.SessionId == sessionId && s.SnapshotType == "current")
            .OrderByDescending(s => s.InteractionIndex)
            .FirstAsync();

        // 反序列化为局面快照（兼容旧格式：缺失字段取默认值）
        var snapshot = DeserializeSnapshot(currentState?.StateJson);

        // 合并变更（仅更新非 null 字段）
        if (changes.Location != null)
            snapshot.Location = changes.Location;
        if (changes.PlayerPosition != null)
            snapshot.PlayerPosition = changes.PlayerPosition;
        if (changes.PlayerStatus != null)
            snapshot.PlayerStatus = changes.PlayerStatus;
        if (changes.Environment != null)
            snapshot.Environment = changes.Environment;
        if (changes.NpcStates != null && changes.NpcStates.Count > 0)
            MergeNpcStates(snapshot.NpcStates, changes.NpcStates);
        if (changes.ActiveConditions != null)
            snapshot.ActiveConditions = changes.ActiveConditions;
        if (changes.Flags != null)
            snapshot.Flags = changes.Flags;
        if (changes.QuestProgress != null)
            snapshot.QuestProgress = changes.QuestProgress;

        // 同步时间字段
        snapshot.CurrentDay = session.CurrentDay;
        snapshot.CurrentSegment = session.CurrentSegment switch
        {
            0 => "上午", 1 => "下午", 2 => "傍晚", 3 => "夜间", _ => "上午"
        };

        // 追加 change_history
        if (!string.IsNullOrWhiteSpace(changes.Summary))
        {
            snapshot.ChangeHistory.Add(new ChangeHistoryEntry
            {
                Round = interactionCount,
                Summary = changes.Summary
            });
        }

        // 将旧的current标记为history
        if (currentState != null)
        {
            currentState.SnapshotType = "history";
            await _worldStateRep.AsUpdateable(currentState)
                .UpdateColumns(s => new { s.SnapshotType })
                .ExecuteCommandAsync();
        }

        // 写入新状态快照
        var newState = new GameWorldState
        {
            SessionId = sessionId,
            StateJson = JsonConvert.SerializeObject(snapshot, Formatting.None),
            SnapshotType = "current",
            InteractionIndex = interactionCount
        };

        await _worldStateRep.AsInsertable(newState).ExecuteCommandAsync();
        return newState;
    }

    /// <summary>
    /// 反序列化StateJson为局面快照（兼容旧格式：缺失字段取默认值）
    /// </summary>
    private static SituationSnapshotDto DeserializeSnapshot(string? stateJson)
    {
        if (string.IsNullOrWhiteSpace(stateJson))
            return new SituationSnapshotDto();

        try
        {
            var snapshot = JsonConvert.DeserializeObject<SituationSnapshotDto>(stateJson);
            return snapshot ?? new SituationSnapshotDto();
        }
        catch
        {
            // 旧格式兼容：尝试将旧JSON中的WorldSetting提取出来
            try
            {
                var oldDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(stateJson);
                var fallback = new SituationSnapshotDto();
                if (oldDict != null && oldDict.TryGetValue("WorldSetting", out var ws))
                    fallback.WorldSetting = JsonConvert.DeserializeObject<Dictionary<string, object>>(ws.ToString() ?? "{}");
                return fallback;
            }
            catch
            {
                return new SituationSnapshotDto();
            }
        }
    }

    /// <summary>
    /// 合并NPC状态（按npc_id匹配更新，新增的追加）
    /// </summary>
    private static void MergeNpcStates(List<NpcStateDto> existing, List<NpcStateDto> updates)
    {
        foreach (var update in updates)
        {
            var match = existing.FirstOrDefault(n => n.NpcId == update.NpcId);
            if (match != null)
            {
                match.Awareness = update.Awareness;
                match.Status = update.Status;
                match.Attitude = update.Attitude;
            }
            else
            {
                existing.Add(update);
            }
        }
    }

    /// <summary>
    /// 获取当前局面快照（分类AI用，过滤掉change_history）
    /// </summary>
    internal async Task<string> GetCurrentStateForClassifierAsync(long sessionId)
    {
        var state = await GetCurrentStateAsync(sessionId);
        var snapshot = DeserializeSnapshot(state.StateJson);
        // 分类AI不需要历史，置空后序列化
        snapshot.ChangeHistory = new List<ChangeHistoryEntry>();
        return JsonConvert.SerializeObject(snapshot, Formatting.None);
    }

    /// <summary>
    /// 获取当前局面快照全量（导演AI用，含change_history）
    /// </summary>
    internal async Task<string> GetCurrentStateForDirectorAsync(long sessionId)
    {
        var state = await GetCurrentStateAsync(sessionId);
        return state.StateJson ?? "{}";
    }

    /// <summary>
    /// 生成角色再定位快照
    /// 包含: 玩家HP/时段/背包/技能 + 核心NPC状态 + 主线进度 + 紧张度
    /// </summary>
    [DisplayName("生成再定位快照")]
    [HttpPost("generateRepositionSnapshot")]
    public async Task<GameWorldState> GenerateRepositionSnapshotApiAsync([FromBody] SessionIdInput input)
        => await GenerateRepositionSnapshotAsync(input.SessionId);

    /// <summary>
    /// 生成再定位快照（内部调用）
    /// </summary>
    internal async Task<GameWorldState> GenerateRepositionSnapshotAsync(long sessionId)
    {
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        var items = character != null
            ? await _itemRep.AsQueryable().Where(i => i.CharacterId == character.Id).ToListAsync()
            : new List<GameInventoryItem>();
        var skills = character != null
            ? await _baseSkillRep.AsQueryable().Where(s => s.CharacterId == character.Id).ToListAsync()
            : new List<GameBaseSkill>();
        var criticalNpcs = await _npcRep.AsQueryable()
            .Where(n => n.SessionId == sessionId && n.IsCritical && n.IsAlive)
            .ToListAsync();

        var snapshot = new
        {
            Player = character != null ? new
            {
                character.CurrentHp,
                character.MaxHp,
                character.Level,
                character.IsWounded,
                character.IsFatigued,
                character.CurrentLocation
            } : null,
            TimeSegment = new
            {
                session.CurrentDay,
                session.CurrentSegment,
                session.OvertimeCount
            },
            Inventory = items.Select(i => new { i.ItemName, i.ItemType, i.Quantity, i.IsKeyItem, i.IsEquipped, i.AttributeBonus, i.LinkedAttribute, i.Weight }),
            Skills = skills.Select(s => new { s.SkillName, s.Level, s.Bonus }),
            CriticalNpcs = criticalNpcs.Select(n => new
            {
                n.NpcIdentifier,
                n.Name,
                n.CurrentAttitude,
                n.Location,
                n.IsAlive
            }),
            MainQuestProgress = session.MainQuest,
            TensionLevel = session.TensionLevel,
            InteractionCount = session.InteractionCount
        };

        var repositionState = new GameWorldState
        {
            SessionId = sessionId,
            StateJson = JsonConvert.SerializeObject(snapshot),
            SnapshotType = "reposition",
            InteractionIndex = session.InteractionCount
        };

        await _worldStateRep.AsInsertable(repositionState).ExecuteCommandAsync();
        return repositionState;
    }

    /// <summary>
    /// 是否需要触发再定位
    /// 每5个交互 或 时段切换 或 长休息后
    /// </summary>
    [DisplayName("是否需要再定位")]
    [HttpGet("shouldReposition")]
    public bool ShouldRepositionApi([FromQuery] ShouldRepositionInput input)
        => ShouldReposition(input.InteractionCount);

    /// <summary>
    /// 是否需要再定位（内部调用）
    /// </summary>
    internal bool ShouldReposition(int interactionCount)
    {
        return interactionCount > 0 && interactionCount % _options.RepositionInterval == 0;
    }

    /// <summary>
    /// 获取最近N条叙事日志
    /// </summary>
    [DisplayName("获取叙事历史")]
    [HttpGet("getNarrativeHistory")]
    public async Task<List<GameNarrativeLog>> GetNarrativeHistoryAsync([FromQuery] NarrativeHistoryQueryInput input)
    {
        var query = _narrativeRep.AsQueryable()
            .Where(n => n.SessionId == input.SessionId);

        if (input.ExcludeAdult)
            query = query.Where(n => !n.IsAdult);

        return await query
            .OrderByDescending(n => n.InteractionIndex)
            .Take(input.Count)
            .ToListAsync();
    }
}

/// <summary>
/// 应用状态变更输入（结构化）
/// </summary>
public class ApplyStateChangesInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>交互轮次</summary>
    public int InteractionCount { get; set; }
    /// <summary>世界状态变更（导演AI输出的结构化变更）</summary>
    public WorldStateChangesDto Changes { get; set; } = new();
}

/// <summary>
/// 叙事历史查询输入
/// </summary>
public class NarrativeHistoryQueryInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>查询条数(默认10)</summary>
    public int Count { get; set; } = 10;
    /// <summary>排除成人内容记录</summary>
    public bool ExcludeAdult { get; set; }
}

/// <summary>
/// 初始化世界状态输入
/// </summary>
public class InitializeWorldStateInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>建筑师AI输出</summary>
    public string ArchitectOutput { get; set; } = "";
}

/// <summary>
/// 是否再定位输入
/// </summary>
public class ShouldRepositionInput
{
    /// <summary>交互次数</summary>
    public int InteractionCount { get; set; }
}
