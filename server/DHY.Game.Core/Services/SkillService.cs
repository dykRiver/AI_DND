using Yitter.IdGenerator;

namespace DHY.Game.Core.Services;

/// <summary>
/// 技能管理服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class SkillService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameBaseSkill> _baseSkillRep;
    private readonly SqlSugarRepository<GameExpertiseSkill> _expertiseRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly ISqlSugarClient _db;
    private readonly GameOptions _options;

    /// <summary>
    /// DND 5e 16项基础技能定义
    /// </summary>
    private static readonly (string Name, string Attribute)[] BaseSkillDefinitions = new[]
    {
        ("运动", "STR"),
        ("体操", "DEX"),
        ("巧手", "DEX"),
        ("隐匿", "DEX"),
        ("奥秘", "INT"),
        ("历史", "INT"),
        ("调查", "INT"),
        ("自然", "INT"),
        ("洞悉", "WIS"),
        ("医药", "WIS"),
        ("感知", "WIS"),
        ("生存", "WIS"),
        ("欺瞒", "CHA"),
        ("威吓", "CHA"),
        ("表演", "CHA"),
        ("说服", "CHA")
    };

    public SkillService(
        SqlSugarRepository<GameBaseSkill> baseSkillRep,
        SqlSugarRepository<GameExpertiseSkill> expertiseRep,
        SqlSugarRepository<GameCharacter> characterRep,
        ISqlSugarClient db,
        IOptions<GameOptions> options)
    {
        _baseSkillRep = baseSkillRep;
        _expertiseRep = expertiseRep;
        _characterRep = characterRep;
        _db = db;
        _options = options.Value;
    }

    /// <summary>
    /// 初始化16项DND基础技能（副本内快照，从Meta层拷贝）
    /// </summary>
    [DisplayName("初始化基础技能")]
    [HttpPost("initializeBaseSkills")]
    public async Task InitializeBaseSkillsAsync([FromBody] CharacterIdInput input)
    {
        var character = await _characterRep.GetByIdAsync(input.CharacterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        await InitializeBaseSkillsInternalAsync(character);
    }

    /// <summary>
    /// 初始化Meta层16项基础技能（玩家永久记录，幂等）
    /// 首次接触玩家时调用，后续结算时回写升级
    /// </summary>
    internal async Task InitializeMetaSkillsAsync(long metaId)
    {
        // 幂等：检查Meta层已存在的技能名
        var existingNames = await _baseSkillRep.AsQueryable()
            .Where(s => s.MetaId == metaId && s.CharacterId == null)
            .Select(s => s.SkillName)
            .ToListAsync();
        var existingSet = new HashSet<string>(existingNames);

        var skills = new List<GameBaseSkill>();
        foreach (var (name, attr) in BaseSkillDefinitions)
        {
            if (existingSet.Contains(name))
                continue;

            skills.Add(new GameBaseSkill
            {
                Id = YitIdHelper.NextId(),
                MetaId = metaId,
                CharacterId = null,
                SkillName = name,
                LinkedAttribute = attr,
                Level = 0,
                Bonus = 0
            });
        }

        if (skills.Count > 0)
            await _db.Insertable(skills).ExecuteCommandAsync();
    }

    /// <summary>
    /// 初始化副本内技能快照（从Meta层拷贝当前Level/Bonus，CharacterId指向本次角色）
    /// </summary>
    internal async Task InitializeCharacterSkillsFromMetaAsync(long metaId, long characterId)
    {
        // 幂等：副本层已存在该角色技能则跳过
        var existingCount = await _baseSkillRep.AsQueryable()
            .Where(s => s.CharacterId == characterId)
            .CountAsync();
        if (existingCount > 0)
            return;

        // 拉Meta层永久技能作为源
        var metaSkills = await _baseSkillRep.AsQueryable()
            .Where(s => s.MetaId == metaId && s.CharacterId == null)
            .ToListAsync();

        // Meta层还没初始化，则先初始化
        if (metaSkills.Count == 0)
        {
            await InitializeMetaSkillsAsync(metaId);
            metaSkills = await _baseSkillRep.AsQueryable()
                .Where(s => s.MetaId == metaId && s.CharacterId == null)
                .ToListAsync();
        }

        var snapshots = metaSkills.Select(m => new GameBaseSkill
        {
            Id = YitIdHelper.NextId(),
            MetaId = metaId,
            CharacterId = characterId,
            SkillName = m.SkillName,
            LinkedAttribute = m.LinkedAttribute,
            Level = m.Level,
            Bonus = m.Bonus
        }).ToList();

        if (snapshots.Count > 0)
            await _db.Insertable(snapshots).ExecuteCommandAsync();
    }

    /// <summary>
    /// 内部初始化方法（用于事务内调用，兼容旧路径，仅插入Level=0/Bonus=0的快照，无Meta层继承）
    /// </summary>
    internal async Task InitializeBaseSkillsInternalAsync(GameCharacter character)
    {
        // 幂等性保护：查询该角色已有的技能名，避免重复插入
        var existingNames = await _baseSkillRep.AsQueryable()
            .Where(s => s.CharacterId == character.Id)
            .Select(s => s.SkillName)
            .ToListAsync();
        var existingSet = new HashSet<string>(existingNames);

        var skills = new List<GameBaseSkill>();
        foreach (var (name, attr) in BaseSkillDefinitions)
        {
            if (existingSet.Contains(name))
                continue; // 已存在则跳过

            skills.Add(new GameBaseSkill
            {
                Id = YitIdHelper.NextId(),
                CharacterId = character.Id,
                SkillName = name,
                LinkedAttribute = attr,
                Level = 0,
                Bonus = 0
            });
        }

        if (skills.Count > 0)
            await _db.Insertable(skills).ExecuteCommandAsync();
    }

    /// <summary>
    /// 基础技能升级 (0→1→2→3)
    /// </summary>
    [DisplayName("升级基础技能")]
    [HttpPost("upgradeBaseSkill")]
    public async Task<GameBaseSkill> UpgradeBaseSkillAsync([FromBody] UpgradeSkillInput input)
    {
        var skill = await _baseSkillRep.GetFirstAsync(s => s.CharacterId == input.CharacterId && s.SkillName == input.SkillName);
        if (skill == null)
            throw Oops.Oh("技能不存在");

        if (skill.Level >= 3)
            throw Oops.Oh("技能已达最高等级(3)");

        skill.Level++;
        skill.Bonus = skill.Level; // 每级+1加值

        await _baseSkillRep.AsUpdateable(skill)
            .UpdateColumns(s => new { s.Level, s.Bonus })
            .ExecuteCommandAsync();

        return skill;
    }

    /// <summary>
    /// 学习专精技能
    /// </summary>
    [DisplayName("学习专精技能")]
    [HttpPost("learnExpertise")]
    public async Task<GameExpertiseSkill> LearnExpertiseAsync([FromBody] LearnExpertiseInput input)
    {
        // 检查Meta层槽位（最多10个）
        var character = await _characterRep.GetByIdAsync(input.CharacterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        var existingCount = await _expertiseRep.AsQueryable()
            .Where(e => e.CharacterId == input.CharacterId && e.IsActive)
            .CountAsync();

        if (existingCount >= _options.MaxExpertiseSlots)
            throw Oops.Oh($"专精技能槽位已满(最多{_options.MaxExpertiseSlots}个)");

        // 检查是否已学习
        var existing = await _expertiseRep.GetFirstAsync(e => e.CharacterId == input.CharacterId && e.SkillName == input.SkillName && e.IsActive);
        if (existing != null)
            throw Oops.Oh("已经学习了该专精技能");

        var expertise = new GameExpertiseSkill
        {
            CharacterId = input.CharacterId,
            SkillName = input.SkillName,
            SkillType = "专精",
            Level = 1,
            LearnSource = input.Source,
            LearnTime = DateTime.Now,
            SlotIndex = existingCount,
            IsActive = true
        };

        await _expertiseRep.AsInsertable(expertise).ExecuteCommandAsync();
        return expertise;
    }

    /// <summary>
    /// 专精技能升级 (Lv1→2→3)
    /// </summary>
    [DisplayName("专精技能升级")]
    [HttpPost("upgradeExpertise")]
    public async Task<GameExpertiseSkill> UpgradeExpertiseAsync([FromBody] UpgradeSkillInput input)
    {
        var expertise = await _expertiseRep.GetFirstAsync(e => e.CharacterId == input.CharacterId && e.SkillName == input.SkillName && e.IsActive);
        if (expertise == null)
            throw Oops.Oh("专精技能不存在或未激活");

        if (expertise.Level >= 3)
            throw Oops.Oh("专精技能已达最高等级(3)");

        expertise.Level++;

        await _expertiseRep.AsUpdateable(expertise)
            .UpdateColumns(e => new { e.Level })
            .ExecuteCommandAsync();

        return expertise;
    }

    /// <summary>
    /// 遗忘专精技能（释放槽位）
    /// </summary>
    [DisplayName("遗忘专精技能")]
    [HttpPost("forgetExpertise")]
    public async Task ForgetExpertiseAsync([FromBody] ForgetExpertiseInput input)
    {
        var expertise = await _expertiseRep.GetFirstAsync(e => e.MetaId == input.MetaId && e.SlotIndex == input.SlotIndex && e.IsActive);
        if (expertise == null)
            throw Oops.Oh("指定槽位无专精技能");

        expertise.IsActive = false;

        await _expertiseRep.AsUpdateable(expertise)
            .UpdateColumns(e => new { e.IsActive })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 副本结束后技能回写Meta层
    /// </summary>
    [DisplayName("同步技能到Meta层")]
    [HttpPost("syncSkillsToMeta")]
    public async Task SyncSkillsToMetaAsync([FromBody] SessionIdInput input)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");

        // 获取副本内专精技能
        var dungeonExpertise = await _expertiseRep.AsQueryable()
            .Where(e => e.CharacterId == character.Id && e.IsActive)
            .ToListAsync();

        // 回写到Meta层（MetaId对应记录）
        foreach (var skill in dungeonExpertise)
        {
            // 查找Meta层同名技能
            var metaSkill = await _expertiseRep.GetFirstAsync(e =>
                e.MetaId != null && e.MetaId > 0 &&
                e.CharacterId == null &&
                e.SkillName == skill.SkillName &&
                e.IsActive);

            if (metaSkill != null)
            {
                // 更新等级（取较高值）
                if (skill.Level > metaSkill.Level)
                {
                    metaSkill.Level = skill.Level;
                    await _expertiseRep.AsUpdateable(metaSkill)
                        .UpdateColumns(e => new { e.Level })
                        .ExecuteCommandAsync();
                }
            }
            // 如果Meta层没有则不自动新增，需通过其他流程处理
        }
    }
}

/// <summary>
/// 技能升级输入
/// </summary>
public class UpgradeSkillInput
{
    /// <summary>角色ID</summary>
    public long CharacterId { get; set; }
    /// <summary>技能名称</summary>
    public string SkillName { get; set; } = "";
}

/// <summary>
/// 学习专精技能输入
/// </summary>
public class LearnExpertiseInput
{
    /// <summary>角色ID</summary>
    public long CharacterId { get; set; }
    /// <summary>技能名称</summary>
    public string SkillName { get; set; } = "";
    /// <summary>学习来源</summary>
    public string? Source { get; set; }
}

/// <summary>
/// 遗忘专精技能输入
/// </summary>
public class ForgetExpertiseInput
{
    /// <summary>Meta档案ID</summary>
    public long MetaId { get; set; }
    /// <summary>槽位索引</summary>
    public int SlotIndex { get; set; }
}
