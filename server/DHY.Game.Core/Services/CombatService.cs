namespace DHY.Game.Core.Services;

/// <summary>
/// 战斗系统服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class CombatService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameDungeonTemplate> _templateRep;
    private readonly DiceService _diceService;
    private readonly JudgmentService _judgmentService;
    private readonly HpService _hpService;

    public CombatService(
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameDungeonTemplate> templateRep,
        DiceService diceService,
        JudgmentService judgmentService,
        HpService hpService)
    {
        _characterRep = characterRep;
        _sessionRep = sessionRep;
        _templateRep = templateRep;
        _diceService = diceService;
        _judgmentService = judgmentService;
        _hpService = hpService;
    }

    /// <summary>
    /// 进入战斗状态
    /// </summary>
    [DisplayName("进入战斗")]
    [HttpPost("enterCombat")]
    public async Task EnterCombatApiAsync(CharacterIdInput input)
    {
        await EnterCombatAsync(input.CharacterId);
    }

    /// <summary>
    /// 进入战斗内部实现
    /// </summary>
    internal async Task EnterCombatAsync(long characterId)
    {
        var character = await _characterRep.GetByIdAsync(characterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        character.IsInCombat = true;

        await _characterRep.AsUpdateable(character)
            .UpdateColumns(c => new { c.IsInCombat })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 退出战斗
    /// </summary>
    [DisplayName("退出战斗")]
    [HttpPost("exitCombat")]
    public async Task ExitCombatApiAsync(CharacterIdInput input)
    {
        await ExitCombatAsync(input.CharacterId);
    }

    /// <summary>
    /// 退出战斗内部实现
    /// </summary>
    internal async Task ExitCombatAsync(long characterId)
    {
        var character = await _characterRep.GetByIdAsync(characterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        character.IsInCombat = false;

        await _characterRep.AsUpdateable(character)
            .UpdateColumns(c => new { c.IsInCombat })
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 基于导演AI蓝图执行战斗判定
    /// </summary>
    [DisplayName("解析战斗行动")]
    [HttpPost("resolveCombatAction")]
    public async Task<CombatActionResult> ResolveCombatActionAsync(CombatActionInput input)
    {
        var session = await _sessionRep.GetFirstAsync(s => s.Id == input.SessionId);
        if (session == null)
            throw Oops.Oh("会话不存在");

        var character = await _characterRep.GetFirstAsync(c => c.SessionId == input.SessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");

        var result = new CombatActionResult
        {
            Action = input.Action,
            CombatIntensity = GetCombatIntensity(session.TensionLevel)
        };

        // 执行技能检定（如果需要）
        if (!string.IsNullOrEmpty(input.SkillName) && input.DC > 0)
        {
            // 查出副本世界难度修正
            var difficultyModifier = 0;
            var template = await _templateRep.GetByIdAsync(session.TemplateId);
            if (template != null)
                difficultyModifier = template.DifficultyModifier;

            var diceRecord = await _judgmentService.SkillCheckAsync(
                input.SessionId, input.SkillName, input.DC,
                input.HasAdvantage, input.HasDisadvantage, difficultyModifier);

            result.SkillCheckResult = diceRecord;
            result.IsSuccess = diceRecord.IsSuccess;
        }
        else
        {
            result.IsSuccess = true; // 无需检定的行动默认成功
        }

        // 应用伤害（如果有）
        if (input.DamageExpression != null && result.IsSuccess)
        {
            var damageResult = _diceService.RollDamage(input.DamageExpression);
            var totalDamage = damageResult.Total;

            // 应用伤害修正
            if (input.DamageModifiers != null)
            {
                foreach (var mod in input.DamageModifiers)
                {
                    totalDamage += mod;
                }
            }

            totalDamage = Math.Max(0, totalDamage);

            if (input.IsPlayerDamage)
            {
                // 玩家受到伤害
                var hpResult = await _hpService.ApplyDamageAsync(character.Id, totalDamage);
                result.DamageDealt = totalDamage;
                result.HpAfterDamage = hpResult.CurrentHp;
            }
            else
            {
                // 玩家造成伤害（对NPC/怪物）
                result.DamageDealt = totalDamage;
            }
        }

        return result;
    }

    /// <summary>
    /// 获取战斗强度分级
    /// 1-3: 低强度(小规模冲突)
    /// 4-6: 中强度(正面对抗)
    /// 7-9: 高强度(生死搏斗)
    /// 10: 极限(Boss级)
    /// </summary>
    [DisplayName("获取战斗强度")]
    [HttpGet("getCombatIntensity")]
    public CombatIntensityInfo GetCombatIntensityApi([FromQuery] GetCombatIntensityInput input)
    {
        return GetCombatIntensity(input.TensionLevel);
    }

    /// <summary>
    /// 获取战斗强度内部实现
    /// </summary>
    internal CombatIntensityInfo GetCombatIntensity(int tensionLevel)
    {
        return tensionLevel switch
        {
            <= 3 => new CombatIntensityInfo { Level = tensionLevel, Grade = "低强度", Description = "小规模冲突" },
            <= 6 => new CombatIntensityInfo { Level = tensionLevel, Grade = "中强度", Description = "正面对抗" },
            <= 9 => new CombatIntensityInfo { Level = tensionLevel, Grade = "高强度", Description = "生死搏斗" },
            _ => new CombatIntensityInfo { Level = tensionLevel, Grade = "极限", Description = "Boss级" }
        };
    }
}

/// <summary>
/// 战斗行动输入
/// </summary>
public class CombatActionInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>行动描述</summary>
    public string Action { get; set; } = "";
    /// <summary>技能名称（可选）</summary>
    public string? SkillName { get; set; }
    /// <summary>难度等级</summary>
    public int DC { get; set; }
    /// <summary>伤害表达式如"2d6+3"</summary>
    public string? DamageExpression { get; set; }
    /// <summary>伤害修正</summary>
    public int[]? DamageModifiers { get; set; }
    /// <summary>是否有优势</summary>
    public bool HasAdvantage { get; set; }
    /// <summary>是否有劣势</summary>
    public bool HasDisadvantage { get; set; }
    /// <summary>是否玩家受伤（true=玩家受伤，false=玩家攻击）</summary>
    public bool IsPlayerDamage { get; set; }
}

/// <summary>
/// 战斗行动结果
/// </summary>
public class CombatActionResult
{
    /// <summary>行动描述</summary>
    public string Action { get; set; } = "";
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }
    /// <summary>技能检定结果</summary>
    public GameDiceRollRecord? SkillCheckResult { get; set; }
    /// <summary>造成/受到的伤害</summary>
    public int DamageDealt { get; set; }
    /// <summary>受伤后HP</summary>
    public int? HpAfterDamage { get; set; }
    /// <summary>战斗强度信息</summary>
    public CombatIntensityInfo CombatIntensity { get; set; } = new();
}

/// <summary>
/// 战斗强度信息
/// </summary>
public class CombatIntensityInfo
{
    /// <summary>紧张度级别</summary>
    public int Level { get; set; }
    /// <summary>强度分级</summary>
    public string Grade { get; set; } = "";
    /// <summary>描述</summary>
    public string Description { get; set; } = "";
}

/// <summary>
/// 获取战斗强度输入
/// </summary>
public class GetCombatIntensityInput
{
    /// <summary>紧张度等级(1-10)</summary>
    public int TensionLevel { get; set; }
}
