namespace DHY.Game.Core.Services;

/// <summary>
/// 判定系统服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class JudgmentService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameBaseSkill> _baseSkillRep;
    private readonly SqlSugarRepository<GameExpertiseSkill> _expertiseRep;
    private readonly SqlSugarRepository<GameDiceRollRecord> _diceRecordRep;
    private readonly DiceService _diceService;
    private readonly InventoryService _inventoryService;

    public JudgmentService(
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameBaseSkill> baseSkillRep,
        SqlSugarRepository<GameExpertiseSkill> expertiseRep,
        SqlSugarRepository<GameDiceRollRecord> diceRecordRep,
        DiceService diceService,
        InventoryService inventoryService)
    {
        _characterRep = characterRep;
        _baseSkillRep = baseSkillRep;
        _expertiseRep = expertiseRep;
        _diceRecordRep = diceRecordRep;
        _diceService = diceService;
        _inventoryService = inventoryService;
    }

    /// <summary>
    /// 技能检定
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="skillName">技能名称</param>
    /// <param name="dc">难度等级（分类AI给出的原始DC）</param>
    /// <param name="hasAdvantage">是否有优势</param>
    /// <param name="hasDisadvantage">是否有劣势</param>
    /// <param name="difficultyModifier">世界难度修正值（副本模板难度对应，默认0）</param>
    [DisplayName("技能检定")]
    [HttpPost("skillCheck")]
    public async Task<GameDiceRollRecord> SkillCheckApiAsync([FromBody] SkillCheckInput input)
        => await SkillCheckAsync(input.SessionId, input.SkillName, input.Dc, input.HasAdvantage, input.HasDisadvantage, input.DifficultyModifier);

    /// <summary>
    /// 技能检定（内部调用）
    /// </summary>
    internal async Task<GameDiceRollRecord> SkillCheckAsync(long sessionId, string skillName, int dc, bool hasAdvantage = false, bool hasDisadvantage = false, int difficultyModifier = 0)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
            throw Oops.Oh("当前会话未找到角色");

        // 诊断日志：检查角色和技能数据
        var skillCount = await _baseSkillRep.AsQueryable()
            .Where(s => s.CharacterId == character.Id)
            .CountAsync();
        if (skillCount == 0)
            LogJudgmentWarning($"角色(CharacterId={character.Id})没有任何技能记录！请检查角色创建时是否初始化了技能");
        else
            LogJudgment($"角色(CharacterId={character.Id})技能数={skillCount}, 属性: STR={character.Strength} DEX={character.Dexterity} CON={character.Constitution} INT={character.Intelligence} WIS={character.Wisdom} CHA={character.Charisma}");

        // 解析技能名（容错匹配）
        var (resolvedSkill, resolvedName) = await ResolveSkillAsync(character.Id, skillName);

        // 获取技能加值
        var skillBonus = await GetSkillBonusAsync(character.Id, resolvedName);

        // 获取属性调整值
        var attributeModifier = 0;
        if (resolvedSkill != null)
        {
            var attributeValue = GetAttributeValue(character, resolvedSkill.LinkedAttribute);
            attributeModifier = GetAttributeModifier(attributeValue);
        }

        // 装备加值（武器/防具匹配当前检定属性时生效，0次装备不生效）
        var equipmentBonus = 0;
        var equipmentBonuses = await _inventoryService.GetEquipmentBonusesAsync(character.Id);
        var currentAttr = resolvedSkill?.LinkedAttribute?.ToUpper() ?? "";
        if (!string.IsNullOrEmpty(currentAttr) && equipmentBonuses.TryGetValue(currentAttr, out var bonus))
        {
            equipmentBonus = bonus;
        }

        // 超载惩罚（负重>=70%，DEX检定-2）
        var encumbrancePenalty = 0;
        if (currentAttr == "DEX")
        {
            var weightCheck = await _inventoryService.CheckWeightAsync(sessionId);
            if (weightCheck.IsEncumbered)
            {
                encumbrancePenalty = -2;
                LogJudgment($"负重惩罚: 背包{weightCheck.CurrentWeight}/{weightCheck.MaxWeight}(≥70%), DEX检定-2");
            }
        }

        // 投掷D20
        var diceResult = _diceService.RollD20Internal(hasAdvantage, hasDisadvantage);

        // 计算总值
        var totalModifier = attributeModifier + skillBonus + equipmentBonus + encumbrancePenalty;
        var total = diceResult.FinalRoll + totalModifier;

        // 世界难度修正：最终DC = 分类AI原始DC + 世界难度修正值
        var effectiveDc = dc + difficultyModifier;
        var isSuccess = diceResult.IsNatural20 || (!diceResult.IsNatural1 && total >= effectiveDc);

        // 自然20永远成功，自然1永远失败
        if (diceResult.IsNatural1)
            isSuccess = false;

        // 记录结果
        var record = new GameDiceRollRecord
        {
            SessionId = sessionId,
            SkillName = resolvedName,
            AttributeName = resolvedSkill?.LinkedAttribute,
            D20Roll = diceResult.FinalRoll,
            Modifier = totalModifier,
            Total = total,
            DC = dc,
            WorldDifficultyModifier = difficultyModifier,
            EffectiveDC = effectiveDc,
            HasAdvantage = hasAdvantage,
            HasDisadvantage = hasDisadvantage,
            IsSuccess = isSuccess,
            IsNatural20 = diceResult.IsNatural20,
            IsNatural1 = diceResult.IsNatural1
        };

        await _diceRecordRep.AsInsertable(record).ExecuteCommandAsync();

        // 战斗判定后扣除已装备武器和防具各1次使用次数
        await _inventoryService.DeductEquipmentUsesAsync(character.Id);

        // 输出完整判定链日志
        var resultTag = record.IsNatural20 ? "★大成功★"
            : record.IsNatural1 ? "x大失败x"
            : record.IsSuccess ? "成功" : "失败";
        var advTag = hasAdvantage ? " [优势]" : hasDisadvantage ? " [劣势]" : "";
        var nameHint = resolvedName != skillName ? $" (原始输入: {skillName})" : "";
        LogJudgment($"技能={resolvedName}{nameHint}, 属性={resolvedSkill?.LinkedAttribute ?? "未知"}, " +
            $"属性调整={attributeModifier:+#;-#;0}, 技能加值={skillBonus:+#;-#;0}, " +
            $"装备加值={equipmentBonus:+#;-#;0}, 负重惩罚={encumbrancePenalty:+#;-#;0}, " +
            $"D20={record.D20Roll}{advTag}, 总加值={totalModifier:+#;-#;0}, " +
            $"总值={record.Total} vs 原始DC{dc}+世界难度{difficultyModifier:+#;-#;0}=有效DC{effectiveDc} → {resultTag}");

        return record;
    }

    /// <summary>
    /// 计算伤害
    /// </summary>
    /// <param name="baseDamage">基础伤害</param>
    /// <param name="modifiers">调整值列表</param>
    [DisplayName("计算伤害")]
    [HttpPost("calculateDamage")]
    public int CalculateDamageApi([FromBody] CalculateDamageInput input)
    {
        return CalculateDamage(input.BaseDamage, input.Modifiers);
    }

    /// <summary>
    /// 计算伤害（内部调用）
    /// </summary>
    internal int CalculateDamage(int baseDamage, int[]? modifiers = null)
    {
        var total = baseDamage;
        if (modifiers != null)
        {
            foreach (var mod in modifiers)
            {
                total += mod;
            }
        }
        return Math.Max(0, total);
    }

    /// <summary>
    /// 获取属性调整值 (value - 10) / 2 取下整
    /// </summary>
    [DisplayName("获取属性调整值")]
    [HttpPost("getAttributeModifier")]
    public int GetAttributeModifierApi([FromBody] GetAttributeModifierInput input)
        => GetAttributeModifier(input.AttributeValue);

    /// <summary>
    /// 获取属性调整值（内部调用）
    /// </summary>
    internal int GetAttributeModifier(int attributeValue)
    {
        return (int)Math.Floor((attributeValue - 10) / 2.0);
    }

    /// <summary>
    /// 获取技能加值（基础技能加值 + 专精加值）
    /// </summary>
    [DisplayName("获取技能加值")]
    [HttpPost("getSkillBonus")]
    public async Task<int> GetSkillBonusApiAsync([FromBody] GetSkillBonusInput input)
        => await GetSkillBonusAsync(input.CharacterId, input.SkillName);

    /// <summary>
    /// 获取技能加值（内部调用）
    /// </summary>
    internal async Task<int> GetSkillBonusAsync(long characterId, string skillName)
    {
        var bonus = 0;

        // 基础技能加值
        var baseSkill = await _baseSkillRep.GetFirstAsync(s => s.CharacterId == characterId && s.SkillName == skillName);
        if (baseSkill != null)
        {
            bonus += baseSkill.Bonus;
        }

        // 专精加值
        var expertise = await _expertiseRep.GetFirstAsync(e => e.CharacterId == characterId && e.SkillName == skillName && e.IsActive);
        if (expertise != null)
        {
            bonus += expertise.Level;
        }

        return bonus;
    }

    /// <summary>
    /// 解析技能名（容错匹配）
    /// </summary>
    /// <returns>(匹配到的技能记录, 解析后的技能名)</returns>
    private async Task<(GameBaseSkill? skill, string resolvedName)> ResolveSkillAsync(long characterId, string rawSkillName)
    {
        // 1. 精确匹配
        var skill = await _baseSkillRep.GetFirstAsync(s => s.CharacterId == characterId && s.SkillName == rawSkillName);
        if (skill != null)
            return (skill, rawSkillName);

        // 2. 容错: 去掉括号内容后匹配，如 "运动(力量)" → "运动"
        var strippedName = System.Text.RegularExpressions.Regex.Replace(rawSkillName, @"[(\(].*?[)\)]", "").Trim();
        if (!string.IsNullOrEmpty(strippedName) && strippedName != rawSkillName)
        {
            skill = await _baseSkillRep.GetFirstAsync(s => s.CharacterId == characterId && s.SkillName == strippedName);
            if (skill != null)
            {
                LogJudgmentWarning($"技能名 '{rawSkillName}' 匹配到标准技能 '{strippedName}'");
                return (skill, strippedName);
            }
        }

        // 3. 容错: 包含匹配，如 "格斗" 在某个技能名中出现
        if (!string.IsNullOrEmpty(strippedName))
        {
            skill = await _baseSkillRep.GetFirstAsync(s => s.CharacterId == characterId && s.SkillName.Contains(strippedName));
            if (skill != null)
            {
                LogJudgmentWarning($"技能名 '{rawSkillName}' 模糊匹配到 '{skill.SkillName}'");
                return (skill, skill.SkillName);
            }
        }

        // 4. 容错: 从括号中提取属性提示，如 "格斗(力量)" → STR
        var attrMatch = System.Text.RegularExpressions.Regex.Match(rawSkillName, @"[(\(]([^)\)]+)[)\)]");
        if (attrMatch.Success)
        {
            var attrHint = MapChineseAttribute(attrMatch.Groups[1].Value);
            if (!string.IsNullOrEmpty(attrHint))
            {
                // 按属性找第一个匹配的技能（降级处理）
                skill = await _baseSkillRep.GetFirstAsync(s => s.CharacterId == characterId && s.LinkedAttribute == attrHint);
                if (skill != null)
                {
                    LogJudgmentWarning($"技能名 '{rawSkillName}' 无法识别，按属性{attrHint}降级匹配到 '{skill.SkillName}'");
                    return (skill, skill.SkillName);
                }
            }
        }

        // 5. 完全无法匹配
        LogJudgmentWarning($"技能名 '{rawSkillName}' 完全无法匹配到标准技能，属性调整和技能加值为0！请检查分类AI输出");
        return (null, rawSkillName);
    }

    /// <summary>
    /// 中文属性名转英文缩写
    /// </summary>
    private static string? MapChineseAttribute(string chinese)
    {
        return chinese switch
        {
            "力量" or "强壮" => "STR",
            "敏捷" or "灵活" => "DEX",
            "体质" or "耐力" => "CON",
            "智力" or "智慧" => "INT",
            "感知" or "意志" => "WIS",
            "魅力" or "社交" => "CHA",
            _ => null
        };
    }

    /// <summary>
    /// 获取角色指定属性值
    /// </summary>
    private int GetAttributeValue(GameCharacter character, string attributeName)
    {
        return attributeName?.ToUpper() switch
        {
            "STR" or "STRENGTH" => character.Strength,
            "DEX" or "DEXTERITY" => character.Dexterity,
            "CON" or "CONSTITUTION" => character.Constitution,
            "INT" or "INTELLIGENCE" => character.Intelligence,
            "WIS" or "WISDOM" => character.Wisdom,
            "CHA" or "CHARISMA" => character.Charisma,
            _ => 10
        };
    }

    #region 日志

    private static readonly object _logLock = new();

    /// <summary>
    /// 判定日志输出（彩色，线程安全）
    /// </summary>
    private static void LogJudgment(string message)
    {
        lock (_logLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"[判定] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ForegroundColor = prevColor;
        }
        GameFileLogger.Write("[判定]", message);
    }

    /// <summary>
    /// 判定警告日志（黄色，用于技能名容错匹配等场景）
    /// </summary>
    private static void LogJudgmentWarning(string message)
    {
        lock (_logLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[判定警告] {message}");
            Console.ForegroundColor = prevColor;
        }
        GameFileLogger.Write("[判定警告]", message);
    }

    #endregion
}

/// <summary>
/// 技能检定输入
/// </summary>
public class SkillCheckInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>技能名称</summary>
    public string SkillName { get; set; } = "";
    /// <summary>难度等级（分类AI给出的原始DC）</summary>
    public int Dc { get; set; }
    /// <summary>是否有优势</summary>
    public bool HasAdvantage { get; set; }
    /// <summary>是否有劣势</summary>
    public bool HasDisadvantage { get; set; }
    /// <summary>世界难度修正值</summary>
    public int DifficultyModifier { get; set; }
}

/// <summary>
/// 计算伤害输入
/// </summary>
public class CalculateDamageInput
{
    /// <summary>基础伤害</summary>
    public int BaseDamage { get; set; }
    /// <summary>调整值列表</summary>
    public int[]? Modifiers { get; set; }
}

/// <summary>
/// 获取属性调整值输入
/// </summary>
public class GetAttributeModifierInput
{
    /// <summary>属性值</summary>
    public int AttributeValue { get; set; }
}

/// <summary>
/// 获取技能加值输入
/// </summary>
public class GetSkillBonusInput
{
    /// <summary>角色ID</summary>
    public long CharacterId { get; set; }
    /// <summary>技能名称</summary>
    public string SkillName { get; set; } = "";
}
