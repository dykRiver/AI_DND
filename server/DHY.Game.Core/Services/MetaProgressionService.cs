namespace DHY.Game.Core.Services;

/// <summary>
/// Meta永久成长服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class MetaProgressionService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GamePlayerMeta> _metaRep;
    private readonly SqlSugarRepository<GameDungeonResult> _resultRep;
    private readonly SqlSugarRepository<GameExpertiseSkill> _expertiseRep;
    private readonly ISqlSugarClient _db;
    private readonly GameOptions _options;

    /// <summary>
    /// 最大Meta等级
    /// </summary>
    private const int MaxMetaLevel = 30;

    /// <summary>
    /// 每项属性Bonus上限
    /// </summary>
    private const int MaxAttributeBonus = 4;

    /// <summary>
    /// 有效属性名称
    /// </summary>
    private static readonly HashSet<string> ValidAttributes = new()
    {
        "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma"
    };

    public MetaProgressionService(
        SqlSugarRepository<GamePlayerMeta> metaRep,
        SqlSugarRepository<GameDungeonResult> resultRep,
        SqlSugarRepository<GameExpertiseSkill> expertiseRep,
        ISqlSugarClient db,
        IOptions<GameOptions> options)
    {
        _metaRep = metaRep;
        _resultRep = resultRep;
        _expertiseRep = expertiseRep;
        _db = db;
        _options = options.Value;
    }

    /// <summary>
    /// 获取Meta档案(若不存在则创建初始Meta)
    /// </summary>
    [DisplayName("获取Meta档案")]
    [HttpGet("getMeta")]
    public async Task<GamePlayerMeta> GetMetaAsync([FromQuery] UserIdInput input)
    {
        var meta = await _metaRep.GetFirstAsync(m => m.UserId == input.UserId);
        if (meta == null)
        {
            meta = new GamePlayerMeta
            {
                UserId = input.UserId,
                MetaLevel = 1,
                Experience = 0,
                BonusStrength = 0,
                BonusDexterity = 0,
                BonusConstitution = 0,
                BonusIntelligence = 0,
                BonusWisdom = 0,
                BonusCharisma = 0,
                TalentPoints = 0,
                DungeonCount = 0
            };
            await _metaRep.AsInsertable(meta).ExecuteCommandAsync();
        }
        return meta;
    }

    /// <summary>
    /// 增加经验值并检查升级
    /// </summary>
    [DisplayName("增加经验值")]
    [HttpPost("addExperience")]
    public async Task<GamePlayerMeta> AddExperienceAsync([FromBody] AddExperienceInput input)
    {
        var meta = await GetMetaAsync(new UserIdInput { UserId = input.UserId });

        meta.Experience += input.Exp;

        // 检查升级: 所需经验 = Level * 100
        while (meta.MetaLevel < MaxMetaLevel)
        {
            var requiredExp = meta.MetaLevel * 100;
            if (meta.Experience >= requiredExp)
            {
                meta.Experience -= requiredExp;
                meta.MetaLevel++;
                meta.TalentPoints++;
            }
            else
            {
                break;
            }
        }

        // 满级后经验值不再增长
        if (meta.MetaLevel >= MaxMetaLevel)
        {
            var maxExp = MaxMetaLevel * 100;
            if (meta.Experience > maxExp)
                meta.Experience = maxExp;
        }

        await _metaRep.AsUpdateable(meta)
            .UpdateColumns(m => new { m.Experience, m.MetaLevel, m.TalentPoints })
            .ExecuteCommandAsync();

        return meta;
    }

    /// <summary>
    /// 分配属性点(来自副本奖励)
    /// </summary>
    [DisplayName("分配属性点")]
    [HttpPost("allocateAttributePoints")]
    public async Task<GamePlayerMeta> AllocateAttributePointsAsync([FromBody] AllocateAttributeInput input)
    {
        if (input.Allocations == null || input.Allocations.Count == 0)
            throw Oops.Oh("分配内容为空");

        var meta = await GetMetaAsync(new UserIdInput { UserId = input.UserId });

        // 计算总分配点数
        var totalPoints = input.Allocations.Values.Sum();

        // 计算可用属性点: 从结算奖励累计(此处简化,直接允许分配)
        // 验证每项不超过上限
        foreach (var (attr, points) in input.Allocations)
        {
            if (!ValidAttributes.Contains(attr))
                throw Oops.Oh($"无效属性名称: {attr}");

            if (points <= 0)
                continue;

            var currentBonus = GetAttributeBonus(meta, attr);
            if (currentBonus + points > MaxAttributeBonus)
                throw Oops.Oh($"{attr} 加成已达上限({MaxAttributeBonus})");
        }

        // 执行分配
        foreach (var (attr, points) in input.Allocations)
        {
            if (points <= 0) continue;
            SetAttributeBonus(meta, attr, GetAttributeBonus(meta, attr) + points);
        }

        await _metaRep.AsUpdateable(meta)
            .UpdateColumns(m => new
            {
                m.BonusStrength,
                m.BonusDexterity,
                m.BonusConstitution,
                m.BonusIntelligence,
                m.BonusWisdom,
                m.BonusCharisma
            })
            .ExecuteCommandAsync();

        return meta;
    }

    /// <summary>
    /// 副本结束后同步Meta
    /// </summary>
    [DisplayName("同步副本结果到Meta")]
    [HttpPost("syncDungeonResult")]
    public async Task SyncDungeonResultAsync([FromBody] SyncDungeonResultInput input)
    {
        var result = await _resultRep.GetFirstAsync(r => r.SessionId == input.SessionId && r.UserId == input.UserId);
        if (result == null)
            throw Oops.Oh("未找到该副本的结算结果");

        try
        {
            _db.AsTenant().BeginTran();

            var meta = await GetMetaAsync(new UserIdInput { UserId = input.UserId });

            // 1. 增加经验值
            meta.Experience += result.RewardMetaExp;
            while (meta.MetaLevel < MaxMetaLevel)
            {
                var requiredExp = meta.MetaLevel * 100;
                if (meta.Experience >= requiredExp)
                {
                    meta.Experience -= requiredExp;
                    meta.MetaLevel++;
                    meta.TalentPoints++;
                }
                else break;
            }

            // 2. 更新DungeonCount
            meta.DungeonCount++;

            await _metaRep.AsUpdateable(meta)
                .UpdateColumns(m => new { m.Experience, m.MetaLevel, m.TalentPoints, m.DungeonCount })
                .ExecuteCommandAsync();

            // 3. 同步专精技能(副本内学到的回写Meta层)
            await SyncExpertiseSkillsAsync(meta.Id, input.SessionId);

            _db.AsTenant().CommitTran();
        }
        catch
        {
            _db.AsTenant().RollbackTran();
            throw;
        }
    }

    /// <summary>
    /// 获取升级进度百分比
    /// </summary>
    [DisplayName("获取升级进度")]
    [HttpGet("getProgressToNextLevel")]
    public async Task<int> GetProgressToNextLevelAsync([FromQuery] UserIdInput input)
    {
        var meta = await GetMetaAsync(input);

        if (meta.MetaLevel >= MaxMetaLevel)
            return 100;

        var requiredExp = meta.MetaLevel * 100;
        return (int)((double)meta.Experience / requiredExp * 100);
    }

    /// <summary>
    /// 获取成长历程摘要
    /// </summary>
    [DisplayName("获取成长历程")]
    [HttpGet("getMetaHistory")]
    public async Task<object> GetMetaHistoryAsync([FromQuery] UserIdInput input)
    {
        var meta = await GetMetaAsync(input);
        var results = await _resultRep.AsQueryable()
            .Where(r => r.UserId == input.UserId)
            .OrderByDescending(r => r.CreateTime)
            .Take(20)
            .ToListAsync();

        return new
        {
            meta.MetaLevel,
            meta.Experience,
            meta.DungeonCount,
            meta.TalentPoints,
            Bonuses = new
            {
                meta.BonusStrength,
                meta.BonusDexterity,
                meta.BonusConstitution,
                meta.BonusIntelligence,
                meta.BonusWisdom,
                meta.BonusCharisma
            },
            RecentResults = results.Select(r => new
            {
                r.SessionId,
                r.ScoreLevel,
                r.TotalScore,
                r.CreateTime
            })
        };
    }

    #region 内部方法

    /// <summary>
    /// 同步专精技能到Meta层
    /// </summary>
    private async Task SyncExpertiseSkillsAsync(long metaId, long sessionId)
    {
        // 获取该会话角色的专精技能
        var dungeonSkills = await _expertiseRep.AsQueryable()
            .Where(e => e.CharacterId != null && e.IsActive)
            .ToListAsync();

        foreach (var skill in dungeonSkills)
        {
            var metaSkill = await _expertiseRep.GetFirstAsync(e =>
                e.MetaId == metaId &&
                e.CharacterId == null &&
                e.SkillName == skill.SkillName &&
                e.IsActive);

            if (metaSkill != null)
            {
                if (skill.Level > metaSkill.Level)
                {
                    metaSkill.Level = skill.Level;
                    await _expertiseRep.AsUpdateable(metaSkill)
                        .UpdateColumns(e => new { e.Level })
                        .ExecuteCommandAsync();
                }
            }
        }
    }

    /// <summary>
    /// 获取属性Bonus值
    /// </summary>
    private static int GetAttributeBonus(GamePlayerMeta meta, string attr)
    {
        return attr switch
        {
            "Strength" => meta.BonusStrength,
            "Dexterity" => meta.BonusDexterity,
            "Constitution" => meta.BonusConstitution,
            "Intelligence" => meta.BonusIntelligence,
            "Wisdom" => meta.BonusWisdom,
            "Charisma" => meta.BonusCharisma,
            _ => 0
        };
    }

    /// <summary>
    /// 设置属性Bonus值
    /// </summary>
    private static void SetAttributeBonus(GamePlayerMeta meta, string attr, int value)
    {
        switch (attr)
        {
            case "Strength": meta.BonusStrength = value; break;
            case "Dexterity": meta.BonusDexterity = value; break;
            case "Constitution": meta.BonusConstitution = value; break;
            case "Intelligence": meta.BonusIntelligence = value; break;
            case "Wisdom": meta.BonusWisdom = value; break;
            case "Charisma": meta.BonusCharisma = value; break;
        }
    }

    #endregion
}

/// <summary>
/// 增加经验输入
/// </summary>
public class AddExperienceInput
{
    /// <summary>用户ID</summary>
    public long UserId { get; set; }
    /// <summary>经验值</summary>
    public int Exp { get; set; }
}

/// <summary>
/// 同步副本结果输入
/// </summary>
public class SyncDungeonResultInput
{
    /// <summary>用户ID</summary>
    public long UserId { get; set; }
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
}
