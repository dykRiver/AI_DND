namespace DHY.Game.Core.Services;

/// <summary>
/// 评分结算系统
/// </summary>
[ApiDescriptionSettings("Game")]
public class ScoringService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameDungeonResult> _resultRep;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameDiceRollRecord> _diceRep;
    private readonly SqlSugarRepository<GameNpcProfile> _npcRep;
    private readonly SqlSugarRepository<GameWorldState> _worldStateRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly WorldStateService _worldStateService;
    private readonly ISqlSugarClient _db;
    private readonly GameOptions _options;

    public ScoringService(
        SqlSugarRepository<GameDungeonResult> resultRep,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameDiceRollRecord> diceRep,
        SqlSugarRepository<GameNpcProfile> npcRep,
        SqlSugarRepository<GameWorldState> worldStateRep,
        SqlSugarRepository<GameCharacter> characterRep,
        WorldStateService worldStateService,
        ISqlSugarClient db,
        IOptions<GameOptions> options)
    {
        _resultRep = resultRep;
        _sessionRep = sessionRep;
        _diceRep = diceRep;
        _npcRep = npcRep;
        _worldStateRep = worldStateRep;
        _characterRep = characterRep;
        _worldStateService = worldStateService;
        _db = db;
        _options = options.Value;
    }

    /// <summary>
    /// 计算副本评分
    /// </summary>
    [DisplayName("计算副本评分")]
    [HttpPost("calculateScore")]
    public async Task<GameDungeonResult> CalculateScoreAsync([FromBody] SessionIdInput input)
    {
        var sessionId = input.SessionId;
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        if (session == null)
            throw Oops.Oh("副本会话不存在");

        // 检查是否已有结算
        var existing = await _resultRep.GetFirstAsync(r => r.SessionId == sessionId);
        if (existing != null)
            return existing;

        // a. MainQuestScore: 基于主线目标完成度（优先读QuestProgress，回退到会话状态）
        var questProgress = await GetQuestProgressAsync(sessionId);
        var mainQuestScore = CalculateMainQuestScore(session, questProgress);

        // b. ExecutionScore: 基于判定成功率和策略质量
        var executionScore = await CalculateExecutionScoreAsync(sessionId);

        // c. ExplorationScore: 基于支线完成和隐藏内容发现（优先读QuestProgress）
        var explorationScore = CalculateExplorationScore(questProgress);

        // d. SurvivalScore: 基于HP保持和濒死次数
        var survivalScore = await CalculateSurvivalScoreAsync(sessionId);

        // e. WorldImpactScore: 基于NPC互动和世界变化
        var worldImpactScore = await CalculateWorldImpactScoreAsync(sessionId);

        // 加权总分
        var weights = _options.ScoringWeights;
        var totalScore = (int)Math.Round(
            mainQuestScore * weights.MainQuest / 100.0 +
            executionScore * weights.Execution / 100.0 +
            explorationScore * weights.Exploration / 100.0 +
            survivalScore * weights.Survival / 100.0 +
            worldImpactScore * weights.WorldImpact / 100.0);

        totalScore = Math.Clamp(totalScore, 0, 100);

        var scoreLevel = MapScoreLevel(totalScore);

        var result = new GameDungeonResult
        {
            SessionId = sessionId,
            UserId = session.UserId,
            ScoreLevel = scoreLevel,
            MainQuestScore = mainQuestScore,
            ExecutionScore = executionScore,
            ExplorationScore = explorationScore,
            SurvivalScore = survivalScore,
            WorldImpactScore = worldImpactScore,
            TotalScore = totalScore
        };

        // 计算奖励
        var rewards = CalculateRewards(scoreLevel);
        result.RewardAttributePoints = rewards.AttributePoints;
        result.RewardSkillPoints = rewards.SkillPoints;
        result.RewardMetaExp = rewards.MetaExp;
        result.RewardTalentFragments = rewards.TalentFragments;

        await _resultRep.AsInsertable(result).ExecuteCommandAsync();
        return result;
    }

    /// <summary>
    /// 计算奖励
    /// </summary>
    [DisplayName("计算奖励")]
    [HttpPost("calculateRewards")]
    public Task<RewardInfo> CalculateRewardsAsync([FromBody] CalculateRewardsInput input)
    {
        return Task.FromResult(CalculateRewards(input.ScoreLevel));
    }

    /// <summary>
    /// 获取已存在的结算结果
    /// </summary>
    [DisplayName("获取结算结果")]
    [HttpGet("getResult")]
    public async Task<GameDungeonResult> GetResultAsync([FromQuery] SessionIdInput input)
    {
        var result = await _resultRep.GetFirstAsync(r => r.SessionId == input.SessionId);
        if (result == null)
            throw Oops.Oh("未找到该会话的结算结果");
        return result;
    }

    /// <summary>
    /// 获取用户最近N次结算
    /// </summary>
    [DisplayName("获取历史结算")]
    [HttpGet("getHistory")]
    public async Task<List<GameDungeonResult>> GetHistoryAsync([FromQuery] ScoringHistoryQueryInput input)
    {
        return await _resultRep.AsQueryable()
            .Where(r => r.UserId == input.UserId)
            .OrderByDescending(r => r.CreateTime)
            .Take(input.Count)
            .ToListAsync();
    }

    #region 评分维度计算

    /// <summary>
    /// 从当前局面快照读取任务进度（QuestProgress）
    /// </summary>
    private async Task<QuestProgressDto?> GetQuestProgressAsync(long sessionId)
    {
        try
        {
            var state = await _worldStateRep.AsQueryable()
                .Where(s => s.SessionId == sessionId && s.SnapshotType == "current")
                .OrderByDescending(s => s.InteractionIndex)
                .FirstAsync();

            if (state == null || string.IsNullOrEmpty(state.StateJson))
                return null;

            var snapshot = Newtonsoft.Json.JsonConvert.DeserializeObject<SituationSnapshotDto>(state.StateJson);
            return snapshot?.QuestProgress;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 主线任务分: 优先读QuestProgress（细粒度），回退到会话状态（粗粒度）
    /// </summary>
    private int CalculateMainQuestScore(GameDungeonSession session, QuestProgressDto? questProgress)
    {
        // 优先使用QuestProgress结构化数据
        if (questProgress != null)
        {
            return questProgress.MainQuestStatus switch
            {
                "complete" => 100,
                "failed" => 10,
                "in_progress" => CalculatePhaseScore(session, questProgress.MainQuestPhase),
                _ => 50
            };
        }

        // 回退：基于会话状态（兼容旧数据）
        return session.Status switch
        {
            1 => 100, // 完全完成
            2 => 15,  // 放弃
            3 => 10,  // 死亡
            _ => 50   // 进行中
        };
    }

    /// <summary>
    /// 根据已完成节点数计算主线进度分
    /// </summary>
    private static int CalculatePhaseScore(GameDungeonSession session, int completedPhase)
    {
        if (completedPhase <= 0) return 20; // 未开始推进但主线进行中

        // 尝试解析总节点数
        var totalNodes = 0;
        if (!string.IsNullOrEmpty(session.MainQuest))
        {
            try
            {
                var mainQuest = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(session.MainQuest);
                if (mainQuest != null && mainQuest.TryGetValue("key_nodes", out var nodesObj))
                {
                    var nodes = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(nodesObj.ToString() ?? "[]");
                    totalNodes = nodes?.Count ?? 0;
                }
            }
            catch { /* 解析失败忽略 */ }
        }

        if (totalNodes <= 0) return 50; // 无法确定总节点数，给默认分

        var ratio = (double)completedPhase / totalNodes;
        return Math.Clamp((int)(ratio * 100), 10, 95); // 进行中上限95，完成才100
    }

    /// <summary>
    /// 探索分: 基于QuestProgress中的支线完成数和隐藏内容发现数
    /// </summary>
    private int CalculateExplorationScore(QuestProgressDto? questProgress)
    {
        if (questProgress == null) return 0; // 无过程数据，默认0

        var score = 0;
        score += (questProgress.CompletedSideQuests?.Count ?? 0) * 25;
        score += (questProgress.DiscoveredHidden?.Count ?? 0) * 15;

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// 执行力分: 骰子判定成功率 + 自然20加分
    /// </summary>
    private async Task<int> CalculateExecutionScoreAsync(long sessionId)
    {
        var rolls = await _diceRep.AsQueryable()
            .Where(r => r.SessionId == sessionId)
            .ToListAsync();

        if (rolls.Count == 0)
            return 50; // 无判定记录默认50

        var successCount = rolls.Count(r => r.IsSuccess);
        var nat20Count = rolls.Count(r => r.IsNatural20);
        var successRate = (double)successCount / rolls.Count;

        // 基础分: 成功率 * 80
        var baseScore = (int)(successRate * 80);
        // 自然20加分: 每次+5,最多+20
        var nat20Bonus = Math.Min(nat20Count * 5, 20);

        return Math.Clamp(baseScore + nat20Bonus, 0, 100);
    }

    /// <summary>
    /// 生存分: 满血完成=100, 每次重伤-15, 每次濒死-25
    /// </summary>
    private async Task<int> CalculateSurvivalScoreAsync(long sessionId)
    {
        var character = await _characterRep.GetFirstAsync(c => c.SessionId == sessionId);
        if (character == null)
            return 50;

        var score = 100;

        // 根据WoundCount扣分(每次重伤-15)
        score -= character.WoundCount * 15;

        // 如果角色当前濒死-25
        if (character.IsDying)
            score -= 25;

        // 如果HP不是满的,按比例扣分
        if (character.MaxHp > 0 && character.CurrentHp < character.MaxHp)
        {
            var hpRatio = (double)character.CurrentHp / character.MaxHp;
            if (hpRatio < 0.5)
                score -= 10;
        }

        return Math.Clamp(score, 0, 100);
    }

    /// <summary>
    /// 世界影响分: NPC正面态度加分 + 世界状态变化加分
    /// </summary>
    private async Task<int> CalculateWorldImpactScoreAsync(long sessionId)
    {
        var score = 0;

        // NPC正面态度(>=+2)加分
        var npcs = await _npcRep.AsQueryable()
            .Where(n => n.SessionId == sessionId)
            .ToListAsync();

        var positiveNpcCount = npcs.Count(n => n.CurrentAttitude >= 2);
        score += positiveNpcCount * 15;

        // 世界状态变化数量加分
        var worldStates = await _worldStateRep.AsQueryable()
            .Where(w => w.SessionId == sessionId)
            .CountAsync();
        score += Math.Min(worldStates * 10, 40);

        return Math.Clamp(score, 0, 100);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    /// 评分等级映射
    /// </summary>
    private static string MapScoreLevel(int totalScore)
    {
        return totalScore switch
        {
            >= 95 => "SSS",
            >= 88 => "SS",
            >= 80 => "S",
            >= 70 => "A",
            >= 60 => "B",
            >= 50 => "C",
            >= 35 => "D",
            >= 20 => "E",
            _ => "F"
        };
    }

    /// <summary>
    /// 根据评分等级计算奖励
    /// </summary>
    private static RewardInfo CalculateRewards(string scoreLevel)
    {
        return scoreLevel switch
        {
            "SSS" => new RewardInfo { AttributePoints = 3, SkillPoints = 3, MetaExp = 200, TalentFragments = 3 },
            "SS" => new RewardInfo { AttributePoints = 2, SkillPoints = 3, MetaExp = 150, TalentFragments = 2 },
            "S" => new RewardInfo { AttributePoints = 2, SkillPoints = 2, MetaExp = 120, TalentFragments = 2 },
            "A" => new RewardInfo { AttributePoints = 1, SkillPoints = 2, MetaExp = 100, TalentFragments = 1 },
            "B" => new RewardInfo { AttributePoints = 1, SkillPoints = 1, MetaExp = 80, TalentFragments = 1 },
            "C" => new RewardInfo { AttributePoints = 0, SkillPoints = 1, MetaExp = 60, TalentFragments = 0 },
            "D" => new RewardInfo { AttributePoints = 0, SkillPoints = 0, MetaExp = 40, TalentFragments = 0 },
            "E" => new RewardInfo { AttributePoints = 0, SkillPoints = 0, MetaExp = 20, TalentFragments = 0 },
            _ => new RewardInfo { AttributePoints = 0, SkillPoints = 0, MetaExp = 10, TalentFragments = 0 }
        };
    }

    #endregion
}

/// <summary>
/// 历史结算查询输入
/// </summary>
public class ScoringHistoryQueryInput
{
    /// <summary>用户ID</summary>
    public long UserId { get; set; }
    /// <summary>查询条数(默认10)</summary>
    public int Count { get; set; } = 10;
}

/// <summary>
/// 计算奖励输入
/// </summary>
public class CalculateRewardsInput
{
    /// <summary>评分等级</summary>
    public string ScoreLevel { get; set; } = "";
}
