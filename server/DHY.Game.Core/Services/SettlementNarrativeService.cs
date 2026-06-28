namespace DHY.Game.Core.Services;

/// <summary>
/// 结算叙事服务(三段式: 叙事退出+后日谈+精简评语)
/// </summary>
[ApiDescriptionSettings("Game")]
public class SettlementNarrativeService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameDungeonResult> _resultRep;
    private readonly SqlSugarRepository<GameDungeonSession> _sessionRep;
    private readonly SqlSugarRepository<GameNpcProfile> _npcRep;
    private readonly SqlSugarRepository<GameCharacter> _characterRep;
    private readonly SqlSugarRepository<GameNarrativeLog> _narrativeLogRep;
    private readonly ISqlSugarClient _db;

    public SettlementNarrativeService(
        SqlSugarRepository<GameDungeonResult> resultRep,
        SqlSugarRepository<GameDungeonSession> sessionRep,
        SqlSugarRepository<GameNpcProfile> npcRep,
        SqlSugarRepository<GameCharacter> characterRep,
        SqlSugarRepository<GameNarrativeLog> narrativeLogRep,
        ISqlSugarClient db)
    {
        _resultRep = resultRep;
        _sessionRep = sessionRep;
        _npcRep = npcRep;
        _characterRep = characterRep;
        _narrativeLogRep = narrativeLogRep;
        _db = db;
    }

    /// <summary>
    /// 生成三段式结算叙事
    /// </summary>
    [DisplayName("生成结算叙事")]
    [HttpPost("generateSettlement")]
    public async Task<SettlementOutput> GenerateSettlementApiAsync(SessionIdInput input)
    {
        return await GenerateSettlementAsync(input.SessionId);
    }

    /// <summary>
    /// 生成结算叙事
    /// </summary>
    public async Task<SettlementOutput> GenerateSettlementAsync(long sessionId)
    {
        var session = await _sessionRep.GetFirstAsync(s => s.Id == sessionId);
        if (session == null)
            throw Oops.Oh("副本会话不存在");

        var result = await _resultRep.GetFirstAsync(r => r.SessionId == sessionId);
        if (result == null)
            throw Oops.Oh("未找到结算结果，请先执行评分");

        // 1. 叙事退出
        var endingType = GetEndingType(session.Status);
        var exitNarrative = await GetSettlementTemplateAsync(endingType);

        // 2. 后日谈
        var epilogue = await GenerateEpilogueAsync(session);

        // 3. 精简评语
        var comment = GenerateComment(result);

        // 回写数据库
        result.EpilogueNarrative = epilogue;
        result.SettlementComment = comment;
        await _resultRep.AsUpdateable(result)
            .UpdateColumns(r => new { r.EpilogueNarrative, r.SettlementComment })
            .ExecuteCommandAsync();

        // 清理叙事日志（结算完成，副本生命周期结束，无需保留）
        await _narrativeLogRep.AsDeleteable()
            .Where(l => l.SessionId == sessionId)
            .ExecuteCommandAsync();

        var rewards = new RewardInfo
        {
            AttributePoints = result.RewardAttributePoints,
            SkillPoints = result.RewardSkillPoints,
            MetaExp = result.RewardMetaExp,
            TalentFragments = result.RewardTalentFragments
        };

        return new SettlementOutput
        {
            ExitNarrative = exitNarrative,
            Epilogue = epilogue,
            Comment = comment,
            ScoreLevel = result.ScoreLevel,
            Rewards = rewards
        };
    }

    /// <summary>
    /// 获取退出叙事模板
    /// </summary>
    [DisplayName("获取退出叙事模板")]
    [HttpGet("getSettlementTemplate")]
    public Task<string> GetSettlementTemplateApiAsync([FromQuery] GetSettlementTemplateInput input)
    {
        return GetSettlementTemplateAsync(input.EndingType);
    }

    /// <summary>
    /// 获取退出叙事模板内部实现
    /// </summary>
    internal Task<string> GetSettlementTemplateAsync(string endingType)
    {
        var template = endingType switch
        {
            "success" => "光芒包裹你的身体，世界渐渐模糊。那些经历过的一切如同退潮的海水，缓缓从意识中褪去。你感到一种温暖的力量将你轻轻托起，带离这个已经完成使命的世界。当光芒散去，你回到了熟悉的虚空之中，心中多了一份沉甸甸的收获。",
            "failure" => "黑暗吞噬了最后一丝光，你感到被一股无形的力量拽出这个世界。那些未完成的遗憾如同碎片般散落，随着你离去的轨迹缓缓消散。虽然没有达成目标，但你带走了教训与经验，下一次将会更加坚定。",
            "death" => "意识如同被打碎的镜面，世界在眼前崩塌成无数碎片。一切感官在瞬间被夺走，只剩下无尽的虚空。但在黑暗的最深处，一丝微光将你从死亡的边缘拉回——这不是终点，只是一次惨痛的教训。你的灵魂带着伤痕回归，等待下一次的觉醒。",
            _ => "世界的边界开始模糊，你感到一股力量将你缓缓拉离。无论结果如何，这段经历已经刻入了你的记忆之中。"
        };

        return Task.FromResult(template);
    }

    #region 内部方法

    /// <summary>
    /// 根据会话状态判断结局类型
    /// </summary>
    private static string GetEndingType(int status)
    {
        return status switch
        {
            1 => "success",
            2 => "failure",
            3 => "death",
            _ => "unknown"
        };
    }

    /// <summary>
    /// 生成后日谈(基于NPC状态和世界变化)
    /// </summary>
    private async Task<string> GenerateEpilogueAsync(GameDungeonSession session)
    {
        var npcs = await _npcRep.AsQueryable()
            .Where(n => n.SessionId == session.Id)
            .ToListAsync();

        var parts = new List<string>();

        // 基于NPC状态生成描述
        foreach (var npc in npcs.Where(n => n.IsCritical))
        {
            if (npc.CurrentAttitude >= 3)
            {
                parts.Add($"你离开后，{npc.Name}时常会提起你的事迹，言语中满是敬意。");
            }
            else if (npc.CurrentAttitude <= -2)
            {
                parts.Add($"{npc.Name}在你离去后松了一口气，但那段经历在其心中留下了难以磨灭的阴影。");
            }
            else if (!npc.IsAlive)
            {
                parts.Add($"{npc.Name}的故事永远停留在了那一刻，人们偶尔会提起这个名字，带着一声叹息。");
            }
        }

        // 基于会话状态补充结局
        if (session.Status == 1)
        {
            parts.Add("这个世界因你的到来而有所改变，即便只是微小的涟漪，也足以影响许多人的命运。");
        }
        else if (session.Status == 3)
        {
            parts.Add("没有人知道那位冒险者最终去了哪里，只有风中偶尔传来的低语，诉说着一段未完的传说。");
        }

        if (parts.Count == 0)
        {
            parts.Add("时间如河流般继续向前，这个世界的齿轮依旧转动着。你的到来或许只是其中短暂的一瞬，但总有些东西在悄然改变。");
        }

        return string.Join("", parts);
    }

    /// <summary>
    /// 生成精简评语(基于评分维度特征)
    /// </summary>
    private static string GenerateComment(GameDungeonResult result)
    {
        // 找出最高和最低维度
        var dimensions = new Dictionary<string, int>
        {
            ["主线"] = result.MainQuestScore,
            ["执行"] = result.ExecutionScore,
            ["探索"] = result.ExplorationScore,
            ["生存"] = result.SurvivalScore,
            ["影响"] = result.WorldImpactScore
        };

        var highest = dimensions.OrderByDescending(d => d.Value).First();
        var lowest = dimensions.OrderBy(d => d.Value).First();

        var strengthDesc = highest.Key switch
        {
            "主线" => "目标明确的执行者",
            "执行" => "技巧出众的行动派",
            "探索" => "充满好奇的探索者",
            "生存" => "谨慎稳重的生存专家",
            "影响" => "善于交际的外交家",
            _ => "冒险者"
        };

        var weaknessDesc = lowest.Key switch
        {
            "主线" => "但有时会偏离主线",
            "执行" => "但缺少一些决断力",
            "探索" => "但对未知之物缺乏好奇",
            "生存" => "但常常忽视自身安危",
            "影响" => "但与世界的联结尚浅",
            _ => ""
        };

        return $"一位{strengthDesc}，{weaknessDesc}。";
    }

    #endregion
}

/// <summary>
/// 获取退出叙事模板输入
/// </summary>
public class GetSettlementTemplateInput
{
    /// <summary>结局类型(success/failure/death/unknown)</summary>
    public string EndingType { get; set; } = "unknown";
}
