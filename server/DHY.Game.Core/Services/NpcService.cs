using Newtonsoft.Json;

namespace DHY.Game.Core.Services;

/// <summary>
/// NPC系统服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class NpcService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameNpcProfile> _npcRep;

    public NpcService(SqlSugarRepository<GameNpcProfile> npcRep)
    {
        _npcRep = npcRep;
    }

    /// <summary>
    /// 创建NPC档案卡
    /// </summary>
    [DisplayName("创建NPC")]
    [HttpPost("createNpc")]
    public async Task<GameNpcProfile> CreateNpcAsync([FromBody] CreateNpcInput input)
    {
        var npc = new GameNpcProfile
        {
            SessionId = input.SessionId,
            NpcIdentifier = input.NpcIdentifier,
            Name = input.Name,
            Role = input.Role,
            Personality = input.Personality,
            Catchphrase = input.Catchphrase,
            LanguageStyle = input.LanguageStyle,
            InitialAttitude = input.InitialAttitude,
            CurrentAttitude = input.InitialAttitude,
            Location = input.Location,
            IsAlive = true,
            ActionPlan = input.ActionPlan,
            IsCritical = input.IsCritical
        };

        await _npcRep.AsInsertable(npc).ExecuteCommandAsync();
        return npc;
    }

    /// <summary>
    /// 获取单个NPC
    /// </summary>
    [DisplayName("获取NPC")]
    [HttpGet("getNpc")]
    public async Task<GameNpcProfile> GetNpcAsync([FromQuery] GetNpcQueryInput input)
    {
        var npc = await _npcRep.GetFirstAsync(n => n.SessionId == input.SessionId && n.NpcIdentifier == input.NpcIdentifier);
        if (npc == null)
            throw Oops.Oh("NPC不存在");
        return npc;
    }

    /// <summary>
    /// 获取会话所有NPC
    /// </summary>
    [DisplayName("获取所有NPC")]
    [HttpGet("getAllNpcs")]
    public async Task<List<GameNpcProfile>> GetAllNpcsAsync([FromQuery] SessionIdInput input)
    {
        return await _npcRep.AsQueryable()
            .Where(n => n.SessionId == input.SessionId)
            .OrderByDescending(n => n.IsCritical)
            .OrderBy(n => n.Name)
            .ToListAsync();
    }

    /// <summary>
    /// 只获取核心NPC
    /// </summary>
    [DisplayName("获取核心NPC")]
    [HttpGet("getCriticalNpcs")]
    public async Task<List<GameNpcProfile>> GetCriticalNpcsApiAsync([FromQuery] SessionIdInput input)
        => await GetCriticalNpcsAsync(input.SessionId);

    /// <summary>
    /// 只获取核心NPC（内部调用）
    /// </summary>
    internal async Task<List<GameNpcProfile>> GetCriticalNpcsAsync(long sessionId)
    {
        return await _npcRep.AsQueryable()
            .Where(n => n.SessionId == sessionId && n.IsCritical)
            .ToListAsync();
    }

    /// <summary>
    /// 更新态度值（限制-5~+5）
    /// </summary>
    [DisplayName("更新NPC态度")]
    [HttpPost("updateAttitude")]
    public async Task<GameNpcProfile> UpdateAttitudeApiAsync([FromBody] UpdateAttitudeInput input)
        => await UpdateAttitudeAsync(input.SessionId, input.NpcId, input.Change);

    /// <summary>
    /// 更新态度值（内部调用）
    /// </summary>
    internal async Task<GameNpcProfile> UpdateAttitudeAsync(long sessionId, long npcId, int change)
    {
        var npc = await _npcRep.GetFirstAsync(n => n.Id == npcId && n.SessionId == sessionId);
        if (npc == null)
            throw Oops.Oh("NPC不存在");

        npc.CurrentAttitude = Math.Clamp(npc.CurrentAttitude + change, -5, 5);

        await _npcRep.AsUpdateable(npc)
            .UpdateColumns(n => new { n.CurrentAttitude })
            .ExecuteCommandAsync();

        return npc;
    }

    /// <summary>
    /// 记录交互摘要
    /// </summary>
    [DisplayName("记录NPC交互")]
    [HttpPost("recordInteraction")]
    public async Task RecordInteractionAsync([FromBody] RecordInteractionInput input)
    {
        var npc = await _npcRep.GetFirstAsync(n => n.Id == input.NpcId && n.SessionId == input.SessionId);
        if (npc == null)
            throw Oops.Oh("NPC不存在");

        // 追加交互记录到JSON数组
        var history = new List<string>();
        if (!string.IsNullOrEmpty(npc.InteractionHistory))
        {
            try
            {
                history = JsonConvert.DeserializeObject<List<string>>(npc.InteractionHistory) ?? new List<string>();
            }
            catch { }
        }

        history.Add($"[{DateTime.Now:HH:mm}] {input.Summary}");
        npc.InteractionHistory = JsonConvert.SerializeObject(history);

        await _npcRep.AsUpdateable(npc)
            .UpdateColumns(n => new { n.InteractionHistory })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 标记NPC死亡
    /// </summary>
    [DisplayName("NPC死亡")]
    [HttpPost("killNpc")]
    public async Task KillNpcAsync([FromBody] SessionNpcInput input)
    {
        var npc = await _npcRep.GetFirstAsync(n => n.Id == input.NpcId && n.SessionId == input.SessionId);
        if (npc == null)
            throw Oops.Oh("NPC不存在");

        npc.IsAlive = false;

        await _npcRep.AsUpdateable(npc)
            .UpdateColumns(n => new { n.IsAlive })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取指定NPC的语言卡片（用于注入叙事AI）
    /// </summary>
    [DisplayName("获取NPC语言卡片")]
    [HttpPost("getLanguageCards")]
    public async Task<List<NpcLanguageCard>> GetLanguageCardsAsync([FromBody] GetLanguageCardsInput input)
    {
        var npcs = await _npcRep.AsQueryable()
            .Where(n => n.SessionId == input.SessionId && input.NpcIdentifiers.Contains(n.NpcIdentifier))
            .ToListAsync();

        return npcs.Select(n => new NpcLanguageCard
        {
            NpcName = n.Name,
            LanguageStyle = n.LanguageStyle ?? "",
            Catchphrase = n.Catchphrase ?? "",
            CurrentAttitude = n.CurrentAttitude
        }).ToList();
    }

    /// <summary>
    /// 生成NPC状态摘要（用于角色再定位）
    /// </summary>
    [DisplayName("生成NPC再定位摘要")]
    [HttpGet("generateRepositionSummary")]
    public async Task<List<NpcRepositionSummary>> GenerateRepositionSummaryAsync([FromQuery] SessionIdInput input)
    {
        var criticalNpcs = await _npcRep.AsQueryable()
            .Where(n => n.SessionId == input.SessionId && n.IsCritical && n.IsAlive)
            .ToListAsync();

        return criticalNpcs.Select(n => new NpcRepositionSummary
        {
            NpcIdentifier = n.NpcIdentifier,
            Name = n.Name,
            CurrentAttitude = n.CurrentAttitude,
            Location = n.Location ?? "未知",
            Role = n.Role ?? ""
        }).ToList();
    }
}

/// <summary>
/// 创建NPC输入
/// </summary>
public class CreateNpcInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>NPC唯一标识</summary>
    public string NpcIdentifier { get; set; } = "";
    /// <summary>NPC名称</summary>
    public string Name { get; set; } = "";
    /// <summary>角色定位</summary>
    public string? Role { get; set; }
    /// <summary>性格标签</summary>
    public string? Personality { get; set; }
    /// <summary>口头禅</summary>
    public string? Catchphrase { get; set; }
    /// <summary>语言风格描述</summary>
    public string? LanguageStyle { get; set; }
    /// <summary>初始态度 (-5~+5)</summary>
    public int InitialAttitude { get; set; }
    /// <summary>所在位置</summary>
    public string? Location { get; set; }
    /// <summary>行动时间线JSON</summary>
    public string? ActionPlan { get; set; }
    /// <summary>是否核心NPC</summary>
    public bool IsCritical { get; set; }
}

/// <summary>
/// NPC语言卡片
/// </summary>
public class NpcLanguageCard
{
    /// <summary>NPC名称</summary>
    public string NpcName { get; set; } = "";
    /// <summary>语言风格</summary>
    public string LanguageStyle { get; set; } = "";
    /// <summary>口头禅</summary>
    public string Catchphrase { get; set; } = "";
    /// <summary>当前态度</summary>
    public int CurrentAttitude { get; set; }
}

/// <summary>
/// NPC再定位摘要
/// </summary>
public class NpcRepositionSummary
{
    /// <summary>NPC标识</summary>
    public string NpcIdentifier { get; set; } = "";
    /// <summary>NPC名称</summary>
    public string Name { get; set; } = "";
    /// <summary>当前态度</summary>
    public int CurrentAttitude { get; set; }
    /// <summary>位置</summary>
    public string Location { get; set; } = "";
    /// <summary>角色定位</summary>
    public string Role { get; set; } = "";
}

/// <summary>
/// 获取NPC查询输入
/// </summary>
public class GetNpcQueryInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>NPC唯一标识</summary>
    public string NpcIdentifier { get; set; } = "";
}

/// <summary>
/// 更新NPC态度输入
/// </summary>
public class UpdateAttitudeInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>NPC ID</summary>
    public long NpcId { get; set; }
    /// <summary>态度变化值</summary>
    public int Change { get; set; }
}

/// <summary>
/// 记录NPC交互输入
/// </summary>
public class RecordInteractionInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>NPC ID</summary>
    public long NpcId { get; set; }
    /// <summary>交互摘要</summary>
    public string Summary { get; set; } = "";
}

/// <summary>
/// 会话NPC操作输入
/// </summary>
public class SessionNpcInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>NPC ID</summary>
    public long NpcId { get; set; }
}

/// <summary>
/// 获取NPC语言卡片输入
/// </summary>
public class GetLanguageCardsInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>NPC标识列表</summary>
    public string[] NpcIdentifiers { get; set; } = Array.Empty<string>();
}
