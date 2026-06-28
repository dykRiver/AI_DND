namespace DHY.Game.Core.Services;

/// <summary>
/// HP系统服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class HpService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly GameOptions _options;

    public HpService(
        SqlSugarRepository<GameCharacter> characterRep,
        IOptions<GameOptions> options)
    {
        _characterRep = characterRep;
        _options = options.Value;
    }

    /// <summary>
    /// 扣血
    /// - 重伤判定: CurrentHp <= MaxHp * WoundThresholdPercent/100
    /// - 濒死判定: CurrentHp <= 0
    /// </summary>
    [DisplayName("扣除HP")]
    [HttpPost("applyDamage")]
    public async Task<HpChangeResult> ApplyDamageApiAsync(ApplyDamageInput input)
    {
        return await ApplyDamageAsync(input.CharacterId, input.Amount);
    }

    /// <summary>
    /// 扣血内部实现
    /// </summary>
    internal async Task<HpChangeResult> ApplyDamageAsync(long characterId, int amount)
    {
        if (amount < 0)
            throw Oops.Oh("伤害值不能为负数");

        var character = await _characterRep.GetByIdAsync(characterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        var previousHp = character.CurrentHp;
        character.CurrentHp -= amount;

        var result = new HpChangeResult
        {
            PreviousHp = previousHp,
            CurrentHp = character.CurrentHp,
            MaxHp = character.MaxHp,
            Change = -amount
        };

        // 重伤判定
        var woundThreshold = (int)Math.Ceiling(character.MaxHp * _options.WoundThresholdPercent / 100.0);
        if (character.CurrentHp <= woundThreshold && character.CurrentHp > 0)
        {
            if (!character.IsWounded)
            {
                character.IsWounded = true;
                character.WoundCount++;
                result.BecameWounded = true;
            }
        }

        // 濒死判定
        if (character.CurrentHp <= 0)
        {
            character.CurrentHp = 0;
            character.IsDying = true;
            result.BecameDying = true;
            result.CurrentHp = 0;
        }

        await _characterRep.AsUpdateable(character)
            .UpdateColumns(c => new { c.CurrentHp, c.IsWounded, c.IsDying, c.WoundCount })
            .ExecuteCommandAsync();

        return result;
    }

    /// <summary>
    /// 治疗（不超过MaxHp）
    /// </summary>
    [DisplayName("治疗HP")]
    [HttpPost("heal")]
    public async Task<HpChangeResult> HealApiAsync(HealInput input)
    {
        return await HealAsync(input.CharacterId, input.Amount);
    }

    /// <summary>
    /// 治疗内部实现
    /// </summary>
    internal async Task<HpChangeResult> HealAsync(long characterId, int amount)
    {
        if (amount < 0)
            throw Oops.Oh("治疗量不能为负数");

        var character = await _characterRep.GetByIdAsync(characterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        var previousHp = character.CurrentHp;
        character.CurrentHp = Math.Min(character.CurrentHp + amount, character.MaxHp);

        // 脱离濒死状态
        if (character.IsDying && character.CurrentHp > 0)
        {
            character.IsDying = false;
        }

        await _characterRep.AsUpdateable(character)
            .UpdateColumns(c => new { c.CurrentHp, c.IsDying })
            .ExecuteCommandAsync();

        return new HpChangeResult
        {
            PreviousHp = previousHp,
            CurrentHp = character.CurrentHp,
            MaxHp = character.MaxHp,
            Change = character.CurrentHp - previousHp
        };
    }

    /// <summary>
    /// 长休息恢复（50%MaxHp向上取整）
    /// </summary>
    [DisplayName("长休息恢复HP")]
    [HttpPost("longRestHeal")]
    public async Task<HpChangeResult> LongRestHealApiAsync(CharacterIdInput input)
    {
        return await LongRestHealAsync(input.CharacterId);
    }

    /// <summary>
    /// 长休息恢复内部实现
    /// </summary>
    internal async Task<HpChangeResult> LongRestHealAsync(long characterId)
    {
        var character = await _characterRep.GetByIdAsync(characterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        var previousHp = character.CurrentHp;
        var healAmount = (int)Math.Ceiling(character.MaxHp * 0.5);
        character.CurrentHp = Math.Min(character.CurrentHp + healAmount, character.MaxHp);

        // 脱离濒死状态
        if (character.IsDying && character.CurrentHp > 0)
        {
            character.IsDying = false;
        }

        await _characterRep.AsUpdateable(character)
            .UpdateColumns(c => new { c.CurrentHp, c.IsDying })
            .ExecuteCommandAsync();

        return new HpChangeResult
        {
            PreviousHp = previousHp,
            CurrentHp = character.CurrentHp,
            MaxHp = character.MaxHp,
            Change = character.CurrentHp - previousHp
        };
    }

    /// <summary>
    /// 获取重伤惩罚值（每次重伤-1, 累计）
    /// </summary>
    [DisplayName("获取重伤惩罚")]
    [HttpGet("getWoundPenalty")]
    public async Task<int> GetWoundPenaltyApiAsync([FromQuery] CharacterIdInput input)
    {
        return await GetWoundPenaltyAsync(input.CharacterId);
    }

    /// <summary>
    /// 获取重伤惩罚内部实现
    /// </summary>
    internal async Task<int> GetWoundPenaltyAsync(long characterId)
    {
        var character = await _characterRep.GetByIdAsync(characterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        return character.IsWounded ? -character.WoundCount : 0;
    }

    /// <summary>
    /// 死亡判定（濒死3回合未救助=死亡）
    /// </summary>
    [DisplayName("死亡判定")]
    [HttpPost("checkDeath")]
    public async Task<DeathCheckResult> CheckDeathApiAsync(CharacterIdInput input)
    {
        return await CheckDeathAsync(input.CharacterId);
    }

    /// <summary>
    /// 死亡判定内部实现
    /// </summary>
    internal async Task<DeathCheckResult> CheckDeathAsync(long characterId)
    {
        var character = await _characterRep.GetByIdAsync(characterId);
        if (character == null)
            throw Oops.Oh("角色不存在");

        var result = new DeathCheckResult
        {
            IsDying = character.IsDying,
            IsDead = false
        };

        // 如果处于濒死状态，判定是否死亡
        // 实际的3回合计数由战斗系统管理，这里提供判定接口
        if (character.IsDying && character.CurrentHp <= 0)
        {
            result.IsDead = true; // 由调用方传入回合数判断
        }

        return result;
    }
}

/// <summary>
/// HP变化结果
/// </summary>
public class HpChangeResult
{
    /// <summary>变化前HP</summary>
    public int PreviousHp { get; set; }
    /// <summary>当前HP</summary>
    public int CurrentHp { get; set; }
    /// <summary>最大HP</summary>
    public int MaxHp { get; set; }
    /// <summary>变化量</summary>
    public int Change { get; set; }
    /// <summary>是否进入重伤</summary>
    public bool BecameWounded { get; set; }
    /// <summary>是否进入濒死</summary>
    public bool BecameDying { get; set; }
}

/// <summary>
/// 死亡判定结果
/// </summary>
public class DeathCheckResult
{
    /// <summary>是否濒死</summary>
    public bool IsDying { get; set; }
    /// <summary>是否死亡</summary>
    public bool IsDead { get; set; }
}

/// <summary>
/// 扣血输入
/// </summary>
public class ApplyDamageInput
{
    /// <summary>角色ID</summary>
    public long CharacterId { get; set; }
    /// <summary>伤害值</summary>
    public int Amount { get; set; }
}

/// <summary>
/// 治疗输入
/// </summary>
public class HealInput
{
    /// <summary>角色ID</summary>
    public long CharacterId { get; set; }
    /// <summary>治疗量</summary>
    public int Amount { get; set; }
}
