namespace DHY.Game.Core.Services;

/// <summary>
/// 段位与晋级赛服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class RankService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GamePlayerRank> _rankRep;
    private readonly SqlSugarRepository<GamePlayerMeta> _metaRep;
    private readonly ISqlSugarClient _db;

    /// <summary>
    /// 段位名称映射
    /// </summary>
    private static readonly Dictionary<int, string> RankNames = new()
    {
        [1] = "青铜",
        [2] = "白银",
        [3] = "黄金",
        [4] = "铂金",
        [5] = "钻石",
        [6] = "大师",
        [7] = "传说"
    };

    /// <summary>
    /// 晋级所需累计副本数
    /// </summary>
    private static readonly Dictionary<int, int> PromotionRequiredDungeons = new()
    {
        [2] = 5,
        [3] = 10,
        [4] = 20,
        [5] = 35,
        [6] = 50,
        [7] = 75
    };

    public RankService(
        SqlSugarRepository<GamePlayerRank> rankRep,
        SqlSugarRepository<GamePlayerMeta> metaRep,
        ISqlSugarClient db)
    {
        _rankRep = rankRep;
        _metaRep = metaRep;
        _db = db;
    }

    /// <summary>
    /// 获取段位(若不存在则创建青铜)
    /// </summary>
    [DisplayName("获取段位")]
    [HttpGet("getRank")]
    public async Task<RankOutput> GetRankAsync([FromQuery] UserIdInput input)
    {
        var rank = await GetOrCreateRankAsync(input.UserId);
        var meta = await _metaRep.GetFirstAsync(m => m.UserId == input.UserId);
        var dungeonCount = meta?.DungeonCount ?? 0;

        var nextTier = rank.RankTier + 1;
        var dungeonCountToNext = 0;
        if (nextTier <= 7 && PromotionRequiredDungeons.ContainsKey(nextTier))
        {
            dungeonCountToNext = Math.Max(0, PromotionRequiredDungeons[nextTier] - dungeonCount);
        }

        return new RankOutput
        {
            RankTier = rank.RankTier,
            RankName = rank.RankName,
            CanPromote = await CanPromoteAsync(input.UserId),
            DungeonCountToNext = dungeonCountToNext,
            IsInPromotion = rank.IsInPromotion
        };
    }

    /// <summary>
    /// 检查是否触发晋级赛
    /// </summary>
    [DisplayName("检查晋级条件")]
    [HttpGet("checkPromotionTrigger")]
    public async Task<object> CheckPromotionTriggerAsync([FromQuery] UserIdInput input)
    {
        var rank = await GetOrCreateRankAsync(input.UserId);
        var meta = await _metaRep.GetFirstAsync(m => m.UserId == input.UserId);

        if (meta == null)
            return new { CanPromote = false, NextRank = "", RequiredScore = "" };

        var canPromote = await CanPromoteAsync(input.UserId);
        var nextTier = rank.RankTier + 1;
        var nextRankName = nextTier <= 7 ? RankNames[nextTier] : "";

        return new
        {
            CanPromote = canPromote,
            NextRank = nextRankName,
            RequiredScore = "B" // 通过条件为B级及以上
        };
    }

    /// <summary>
    /// 开始晋级赛
    /// </summary>
    [DisplayName("开始晋级赛")]
    [HttpPost("startPromotion")]
    public async Task<object> StartPromotionAsync([FromBody] UserIdInput input)
    {
        var rank = await GetOrCreateRankAsync(input.UserId);

        if (rank.IsInPromotion)
            throw Oops.Oh("已经在晋级赛中");

        if (!await CanPromoteAsync(input.UserId))
            throw Oops.Oh("未满足晋级条件");

        rank.IsInPromotion = true;
        rank.PromotionAttempts++;

        await _rankRep.AsUpdateable(rank)
            .UpdateColumns(r => new { r.IsInPromotion, r.PromotionAttempts })
            .ExecuteCommandAsync();

        var nextTier = rank.RankTier + 1;
        return new
        {
            IsInPromotion = true,
            RequiredDifficulty = nextTier, // 难度为当前段位+1
            NextRankName = nextTier <= 7 ? RankNames[nextTier] : "未知",
            PromotionAttempts = rank.PromotionAttempts,
            Message = $"晋级赛已开始! 完成一个难度{nextTier}的考核副本,评分B级以上即可晋级到{(nextTier <= 7 ? RankNames[nextTier] : "未知")}段位。"
        };
    }

    /// <summary>
    /// 晋级赛结果判定
    /// </summary>
    [DisplayName("晋级赛结果")]
    [HttpPost("completePromotion")]
    public async Task<PromotionResultOutput> CompletePromotionAsync([FromBody] CompletePromotionInput input)
    {
        var rank = await GetOrCreateRankAsync(input.UserId);

        if (!rank.IsInPromotion)
            throw Oops.Oh("当前不在晋级赛中");

        // 通过条件: 评分 >= B (B/A/S/SS/SSS)
        var passingLevels = new HashSet<string> { "B", "A", "S", "SS", "SSS" };
        var isSuccess = passingLevels.Contains(input.ScoreLevel);

        rank.IsInPromotion = false;

        if (isSuccess)
        {
            rank.RankTier++;
            rank.RankName = RankNames.GetValueOrDefault(rank.RankTier, "未知");
            rank.LastPromotionTime = DateTime.Now;
            rank.PromotionAttempts = 0;

            await _rankRep.AsUpdateable(rank)
                .UpdateColumns(r => new { r.IsInPromotion, r.RankTier, r.RankName, r.LastPromotionTime, r.PromotionAttempts })
                .ExecuteCommandAsync();

            return new PromotionResultOutput
            {
                IsSuccess = true,
                NewRankTier = rank.RankTier,
                NewRankName = rank.RankName,
                Message = $"恭喜晋级到{rank.RankName}段位!"
            };
        }
        else
        {
            await _rankRep.AsUpdateable(rank)
                .UpdateColumns(r => new { r.IsInPromotion })
                .ExecuteCommandAsync();

            var message = "晋级失败，继续努力!";
            if (rank.PromotionAttempts >= 3)
                message = "连续3次晋级失败，建议降低难度或提升实力后再尝试。";

            return new PromotionResultOutput
            {
                IsSuccess = false,
                NewRankTier = rank.RankTier,
                NewRankName = rank.RankName,
                Message = message
            };
        }
    }

    /// <summary>
    /// 获取段位变化历史
    /// </summary>
    [DisplayName("获取段位历史")]
    [HttpGet("getRankHistory")]
    public async Task<object> GetRankHistoryAsync([FromQuery] UserIdInput input)
    {
        var rank = await GetOrCreateRankAsync(input.UserId);
        return new
        {
            rank.RankTier,
            rank.RankName,
            rank.PromotionAttempts,
            rank.LastPromotionTime,
            rank.IsInPromotion
        };
    }

    /// <summary>
    /// 获取下一级晋级要求
    /// </summary>
    [DisplayName("获取晋级要求")]
    [HttpGet("getPromotionRequirements")]
    public Task<object> GetPromotionRequirementsAsync([FromQuery] CurrentTierInput input)
    {
        var nextTier = input.CurrentTier + 1;
        if (nextTier > 7)
        {
            return Task.FromResult<object>(new
            {
                NextTier = 0,
                NextRankName = "已达最高段位",
                RequiredDungeons = 0,
                RequiredScore = "",
                Message = "已达到最高段位[传说]"
            });
        }

        var requiredDungeons = PromotionRequiredDungeons.GetValueOrDefault(nextTier, 0);
        return Task.FromResult<object>(new
        {
            NextTier = nextTier,
            NextRankName = RankNames[nextTier],
            RequiredDungeons = requiredDungeons,
            RequiredScore = "B",
            Message = $"累计完成{requiredDungeons}个副本后可触发晋级赛，考核副本评分B级以上通过。"
        });
    }

    #region 内部方法

    /// <summary>
    /// 获取或创建段位记录
    /// </summary>
    private async Task<GamePlayerRank> GetOrCreateRankAsync(long userId)
    {
        var rank = await _rankRep.GetFirstAsync(r => r.UserId == userId);
        if (rank == null)
        {
            var meta = await _metaRep.GetFirstAsync(m => m.UserId == userId);
            rank = new GamePlayerRank
            {
                UserId = userId,
                MetaId = meta?.Id ?? 0,
                RankTier = 1,
                RankName = "青铜",
                PromotionDungeonCount = 5,
                IsInPromotion = false,
                PromotionAttempts = 0
            };
            await _rankRep.AsInsertable(rank).ExecuteCommandAsync();
        }
        return rank;
    }

    /// <summary>
    /// 判断是否可以晋级
    /// </summary>
    private async Task<bool> CanPromoteAsync(long userId)
    {
        var rank = await _rankRep.GetFirstAsync(r => r.UserId == userId);
        if (rank == null || rank.RankTier >= 7 || rank.IsInPromotion)
            return false;

        var meta = await _metaRep.GetFirstAsync(m => m.UserId == userId);
        if (meta == null)
            return false;

        var nextTier = rank.RankTier + 1;
        if (!PromotionRequiredDungeons.ContainsKey(nextTier))
            return false;

        // 每5副本检查,且DungeonCount >= 下一段位要求
        return meta.DungeonCount >= PromotionRequiredDungeons[nextTier]
               && meta.DungeonCount % 5 == 0;
    }

    #endregion
}

/// <summary>
/// 完成晋级赛输入
/// </summary>
public class CompletePromotionInput
{
    /// <summary>用户ID</summary>
    public long UserId { get; set; }
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
    /// <summary>评分等级</summary>
    public string ScoreLevel { get; set; } = "";
}

/// <summary>
/// 当前段位输入
/// </summary>
public class CurrentTierInput
{
    /// <summary>当前段位</summary>
    public int CurrentTier { get; set; }
}
