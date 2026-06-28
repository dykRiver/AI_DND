using DHY.Game.Admin.Dtos;
using DHY.Game.Core.Entities;

namespace DHY.Game.Admin.Services;

/// <summary>
/// Token消耗统计服务
/// </summary>
[ApiDescriptionSettings("GameAdmin")]
public class TokenUsageService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<GameAiCallLog> _aiCallLogRep;

    public TokenUsageService(SqlSugarRepository<GameAiCallLog> aiCallLogRep)
    {
        _aiCallLogRep = aiCallLogRep;
    }

    /// <summary>
    /// 时间范围内的Token汇总
    /// </summary>
    [DisplayName("获取Token使用汇总")]
    [HttpGet("usageSummary")]
    public async Task<TokenUsageSummaryOutput> GetUsageSummaryAsync([FromQuery] DateRangeQueryInput input)
    {
        var query = _aiCallLogRep.AsQueryable()
            .Where(c => c.CreateTime >= input.StartDate.Date && c.CreateTime < input.EndDate.Date.AddDays(1) && !c.IsDelete);

        // 总计
        var summary = await query
            .Select(c => new
            {
                TotalInput = SqlFunc.AggregateSum((long)c.InputTokens),
                TotalOutput = SqlFunc.AggregateSum((long)c.OutputTokens),
                TotalCost = SqlFunc.AggregateSum(c.Cost)
            })
            .FirstAsync();

        // 按模型分组
        var byModel = await query
            .GroupBy(c => c.ModelName)
            .Select(c => new ModelUsageItem
            {
                ModelName = c.ModelName,
                CallCount = SqlFunc.AggregateCount(c.Id),
                InputTokens = SqlFunc.AggregateSum((long)c.InputTokens),
                OutputTokens = SqlFunc.AggregateSum((long)c.OutputTokens),
                Cost = SqlFunc.AggregateSum(c.Cost)
            })
            .ToListAsync();

        return new TokenUsageSummaryOutput
        {
            TotalInputTokens = summary?.TotalInput ?? 0,
            TotalOutputTokens = summary?.TotalOutput ?? 0,
            TotalCost = summary?.TotalCost ?? 0,
            ByModel = byModel
        };
    }

    /// <summary>
    /// 按模型维度统计
    /// </summary>
    [DisplayName("按模型统计Token使用")]
    [HttpGet("usageByModel")]
    public async Task<List<ModelUsageItem>> GetUsageByModelAsync([FromQuery] DateRangeQueryInput input)
    {
        return await _aiCallLogRep.AsQueryable()
            .Where(c => c.CreateTime >= input.StartDate.Date && c.CreateTime < input.EndDate.Date.AddDays(1) && !c.IsDelete)
            .GroupBy(c => c.ModelName)
            .Select(c => new ModelUsageItem
            {
                ModelName = c.ModelName,
                CallCount = SqlFunc.AggregateCount(c.Id),
                InputTokens = SqlFunc.AggregateSum((long)c.InputTokens),
                OutputTokens = SqlFunc.AggregateSum((long)c.OutputTokens),
                Cost = SqlFunc.AggregateSum(c.Cost)
            })
            .ToListAsync();
    }

    /// <summary>
    /// 按AI角色维度统计
    /// </summary>
    [DisplayName("按AI角色统计Token使用")]
    [HttpGet("usageByAiType")]
    public async Task<List<ModelUsageItem>> GetUsageByAiTypeAsync([FromQuery] DateRangeQueryInput input)
    {
        return await _aiCallLogRep.AsQueryable()
            .Where(c => c.CreateTime >= input.StartDate.Date && c.CreateTime < input.EndDate.Date.AddDays(1) && !c.IsDelete)
            .GroupBy(c => c.AiType)
            .Select(c => new ModelUsageItem
            {
                ModelName = c.AiType,
                CallCount = SqlFunc.AggregateCount(c.Id),
                InputTokens = SqlFunc.AggregateSum((long)c.InputTokens),
                OutputTokens = SqlFunc.AggregateSum((long)c.OutputTokens),
                Cost = SqlFunc.AggregateSum(c.Cost)
            })
            .ToListAsync();
    }

    /// <summary>
    /// 最近N天每日Token消耗趋势
    /// </summary>
    [DisplayName("获取Token消耗趋势")]
    [HttpGet("usageTrend")]
    public async Task<List<UsageTrendItem>> GetUsageTrendAsync([FromQuery] TrendQueryInput input)
    {
        var days = input.Days;
        if (days <= 0) days = 7;
        if (days > 90) days = 90;

        var startDate = DateTime.Now.Date.AddDays(-days);

        return await _aiCallLogRep.AsQueryable()
            .Where(c => c.CreateTime >= startDate && !c.IsDelete)
            .GroupBy(c => SqlFunc.DateValue(c.CreateTime!.Value, DateType.Year))
            .GroupBy(c => SqlFunc.DateValue(c.CreateTime!.Value, DateType.Month))
            .GroupBy(c => SqlFunc.DateValue(c.CreateTime!.Value, DateType.Day))
            .Select(c => new UsageTrendItem
            {
                Date = Convert.ToDateTime(SqlFunc.MergeString(
                    SqlFunc.DateValue(c.CreateTime!.Value, DateType.Year).ToString(), "-",
                    SqlFunc.DateValue(c.CreateTime!.Value, DateType.Month).ToString(), "-",
                    SqlFunc.DateValue(c.CreateTime!.Value, DateType.Day).ToString())),
                TotalTokens = SqlFunc.AggregateSum((long)c.TotalTokens),
                Cost = SqlFunc.AggregateSum(c.Cost),
                CallCount = SqlFunc.AggregateCount(c.Id)
            })
            .OrderBy(c => c.Date)
            .ToListAsync();
    }

    /// <summary>
    /// 月度费用预估(基于近7天平均消耗)
    /// </summary>
    [DisplayName("获取月度费用预估")]
    [HttpGet("costEstimate")]
    public async Task<CostEstimateOutput> GetCostEstimateAsync()
    {
        var last7Days = DateTime.Now.Date.AddDays(-7);

        var stats = await _aiCallLogRep.AsQueryable()
            .Where(c => c.CreateTime >= last7Days && !c.IsDelete)
            .Select(c => new
            {
                TotalCost = SqlFunc.AggregateSum(c.Cost),
                TotalTokens = SqlFunc.AggregateSum((long)c.TotalTokens)
            })
            .FirstAsync();

        var dailyAvgCost = (stats?.TotalCost ?? 0) / 7m;
        var dailyAvgTokens = (stats?.TotalTokens ?? 0) / 7;

        return new CostEstimateOutput
        {
            DailyAvgCost = Math.Round(dailyAvgCost, 4),
            MonthlyEstimate = Math.Round(dailyAvgCost * 30, 2),
            DailyAvgTokens = dailyAvgTokens
        };
    }

    /// <summary>
    /// AI调用错误率统计
    /// </summary>
    [DisplayName("获取AI调用错误率")]
    [HttpGet("errorRate")]
    public async Task<ErrorRateOutput> GetErrorRateAsync([FromQuery] DateRangeQueryInput input)
    {
        var query = _aiCallLogRep.AsQueryable()
            .Where(c => c.CreateTime >= input.StartDate.Date && c.CreateTime < input.EndDate.Date.AddDays(1) && !c.IsDelete);

        var totalCalls = await query.CountAsync();
        var failedCalls = await query.Where(c => !c.IsSuccess).CountAsync();

        // 按错误类型分组
        var byType = await query
            .Where(c => !c.IsSuccess && c.ErrorMessage != null)
            .GroupBy(c => c.ErrorMessage!)
            .Select(c => new ErrorTypeItem
            {
                ErrorType = c.ErrorMessage!,
                Count = SqlFunc.AggregateCount(c.Id)
            })
            .OrderByDescending(c => c.Count)
            .Take(10)
            .ToListAsync();

        return new ErrorRateOutput
        {
            TotalCalls = totalCalls,
            FailedCalls = failedCalls,
            ErrorRate = totalCalls > 0 ? Math.Round((double)failedCalls / totalCalls * 100, 2) : 0,
            ByType = byType
        };
    }
}
