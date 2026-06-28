namespace DHY.Game.Core.Services;

/// <summary>
/// 角色管理服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class CharacterService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly ISqlSugarClient _db;
    private readonly GameOptions _options;
    private readonly SkillService _skillService;
    private readonly JudgmentService _judgmentService;

    public CharacterService(
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        ISqlSugarClient db,
        IOptions<GameOptions> options,
        SkillService skillService,
        JudgmentService judgmentService)
    {
        _characterRep = characterRep;
        _sessionRep = sessionRep;
        _db = db;
        _options = options.Value;
        _skillService = skillService;
        _judgmentService = judgmentService;
    }

    /// <summary>
    /// 创建副本内角色
    /// 使用点买法，总点数27
    /// </summary>
    [DisplayName("创建副本角色")]
    [HttpPost("createCharacter")]
    public async Task<GameCharacter> CreateCharacterAsync(CreateCharacterInput input)
    {
        // 验证点买法：总点数27
        var pointCost = CalculatePointBuyCost(input.Strength)
            + CalculatePointBuyCost(input.Dexterity)
            + CalculatePointBuyCost(input.Constitution)
            + CalculatePointBuyCost(input.Intelligence)
            + CalculatePointBuyCost(input.Wisdom)
            + CalculatePointBuyCost(input.Charisma);

        if (pointCost > 27)
            throw Oops.Oh("属性点数超出限制(最多27点)");

        // 计算MaxHp
        var conModifier = _judgmentService.GetAttributeModifier(input.Constitution);
        var maxHp = _options.MaxBaseHp + conModifier * _options.HpPerConModifier;
        if (maxHp < 1) maxHp = 1;

        // 计算背包容量上限: 15 + STR调整值
        var strModifier = _judgmentService.GetAttributeModifier(input.Strength);
        var weightCapacity = 15 + strModifier;

        var character = new GameCharacter
        {
            UserId = input.UserId,
            SessionId = input.SessionId,
            Name = input.Name,
            Strength = input.Strength,
            Dexterity = input.Dexterity,
            Constitution = input.Constitution,
            Intelligence = input.Intelligence,
            Wisdom = input.Wisdom,
            Charisma = input.Charisma,
            MaxHp = maxHp,
            CurrentHp = maxHp,
            Level = 1,
            IsInCombat = false,
            IsFatigued = false,
            IsWounded = false,
            IsDying = false,
            WoundCount = 0,
            WeightCapacity = weightCapacity
        };

        try
        {
            _db.AsTenant().BeginTran();

            var newId = await _db.Insertable(character).ExecuteReturnSnowflakeIdAsync();
            character.Id = newId;

            // 初始化基础技能
            await _skillService.InitializeBaseSkillsInternalAsync(character);

            _db.AsTenant().CommitTran();
        }
        catch
        {
            _db.AsTenant().RollbackTran();
            throw;
        }

        return character;
    }

    /// <summary>
    /// [测试用] 重建角色信息（修复历史数据 + 重建技能）
    /// </summary>
    [DisplayName("重建角色信息")]
    [HttpPost("reinitCharacter")]
    public async Task<string> ReinitCharacterApiAsync(SessionIdInput input)
    {
        return await ReinitCharacterAsync(input.SessionId);
    }

    /// <summary>
    /// 重建角色信息内部实现（修复 WeightCapacity/MaxHp 等历史数据 + 重建技能）
    /// </summary>
    internal async Task<string> ReinitCharacterAsync(long sessionId)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");

        try
        {
            _db.AsTenant().BeginTran();

            // ---------- 1. 修复角色历史数据 ----------
            // 重算背包容量上限: 15 + STR调整值
            var strModifier = _judgmentService.GetAttributeModifier(character.Strength);
            var weightCapacity = 15 + strModifier;
            character.WeightCapacity = weightCapacity;

            // 重算MaxHp: 基础HP + 等级增量（与 CreateCharacterAsync + LevelUpAsync 逻辑一致）
            var conModifier = _judgmentService.GetAttributeModifier(character.Constitution);
            var baseHp = _options.MaxBaseHp + conModifier * _options.HpPerConModifier;
            if (baseHp < 1) baseHp = 1;
            var levelHp = (character.Level - 1) * (5 + Math.Max(0, conModifier));
            var maxHp = baseHp + levelHp;
            character.MaxHp = maxHp;
            // CurrentHp 不主动回满，仅截断不超过新上限
            character.CurrentHp = Math.Min(character.CurrentHp, maxHp);

            // 更新角色记录（仅更新需要修复的字段）
            await _characterRep.AsUpdateable(character)
                .UpdateColumns(c => new { c.WeightCapacity, c.MaxHp, c.CurrentHp })
                .ExecuteCommandAsync();

            // ---------- 2. 重建技能 ----------
            // 删除该角色所有旧技能
            var deleted = await _db.Deleteable<GameBaseSkill>()
                .Where(s => s.CharacterId == character.Id)
                .ExecuteCommandAsync();

            // 重新初始化16项技能
            await _skillService.InitializeBaseSkillsInternalAsync(character);

            _db.AsTenant().CommitTran();

            return $"角色({character.Name})信息已重建：WeightCapacity={weightCapacity}，MaxHp={maxHp}，技能删除{deleted}条并重新初始化16条";
        }
        catch
        {
            _db.AsTenant().RollbackTran();
            throw;
        }
    }

    /// <summary>
    /// 获取当前角色完整信息
    /// </summary>
    [DisplayName("获取当前角色")]
    [HttpGet("getCharacter")]
    public async Task<GameCharacter> GetCharacterApiAsync([FromQuery] SessionIdInput input)
    {
        return await GetCharacterAsync(input.SessionId);
    }

    /// <summary>
    /// 获取当前角色内部实现
    /// </summary>
    internal async Task<GameCharacter> GetCharacterAsync(long sessionId)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");
        return character;
    }

    /// <summary>
    /// 更新角色状态
    /// </summary>
    [DisplayName("更新角色状态")]
    [HttpPost("updateCharacterStatus")]
    public async Task UpdateCharacterStatusAsync(UpdateCharacterStatusInput input)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");

        if (input.CurrentHp.HasValue) character.CurrentHp = input.CurrentHp.Value;
        if (input.IsInCombat.HasValue) character.IsInCombat = input.IsInCombat.Value;
        if (input.IsFatigued.HasValue) character.IsFatigued = input.IsFatigued.Value;
        if (input.IsWounded.HasValue) character.IsWounded = input.IsWounded.Value;
        if (input.IsDying.HasValue) character.IsDying = input.IsDying.Value;
        if (input.CurrentLocation != null) character.CurrentLocation = input.CurrentLocation;

        await _characterRep.AsUpdateable(character).ExecuteCommandAsync();
    }

    /// <summary>
    /// 里程碑升级 (1→2→3→4)
    /// HP增加, 技能点分配
    /// </summary>
    [DisplayName("角色升级")]
    [HttpPost("levelUp")]
    public async Task<GameCharacter> LevelUpApiAsync(SessionIdInput input)
    {
        return await LevelUpAsync(input.SessionId);
    }

    /// <summary>
    /// 角色升级内部实现
    /// </summary>
    internal async Task<GameCharacter> LevelUpAsync(long sessionId)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");

        if (character.Level >= _options.MaxDungeonLevel)
            throw Oops.Oh($"已达到副本内最大等级({_options.MaxDungeonLevel})");

        character.Level++;

        // HP增加：每级增加基础值 + CON调整值
        var conModifier = _judgmentService.GetAttributeModifier(character.Constitution);
        var hpIncrease = 5 + Math.Max(0, conModifier); // 每级+5基础 + CON调整值
        character.MaxHp += hpIncrease;
        character.CurrentHp += hpIncrease;

        await _characterRep.AsUpdateable(character)
            .UpdateColumns(c => new { c.Level, c.MaxHp, c.CurrentHp })
            .ExecuteCommandAsync();

        return character;
    }

    /// <summary>
    /// 点买法点数花费计算 (8=0点, 9=1点...13=5点, 14=7点, 15=9点)
    /// </summary>
    private int CalculatePointBuyCost(int value)
    {
        if (value < 8 || value > 15)
            throw Oops.Oh($"属性值必须在8-15之间，当前值: {value}");

        return value switch
        {
            8 => 0,
            9 => 1,
            10 => 2,
            11 => 3,
            12 => 4,
            13 => 5,
            14 => 7,
            15 => 9,
            _ => 0
        };
    }
}

/// <summary>
/// 创建角色输入
/// </summary>
public class CreateCharacterInput
{
    /// <summary>用户ID</summary>
    public long UserId { get; set; }
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>角色名称</summary>
    public string Name { get; set; } = "";
    /// <summary>力量 (8-15)</summary>
    public int Strength { get; set; }
    /// <summary>敏捷 (8-15)</summary>
    public int Dexterity { get; set; }
    /// <summary>体质 (8-15)</summary>
    public int Constitution { get; set; }
    /// <summary>智力 (8-15)</summary>
    public int Intelligence { get; set; }
    /// <summary>感知 (8-15)</summary>
    public int Wisdom { get; set; }
    /// <summary>魅力 (8-15)</summary>
    public int Charisma { get; set; }
}

/// <summary>
/// 更新角色状态输入
/// </summary>
public class UpdateCharacterStatusInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    public int? CurrentHp { get; set; }
    public bool? IsInCombat { get; set; }
    public bool? IsFatigued { get; set; }
    public bool? IsWounded { get; set; }
    public bool? IsDying { get; set; }
    public string? CurrentLocation { get; set; }
}
