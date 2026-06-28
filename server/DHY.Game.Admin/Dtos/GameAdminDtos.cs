namespace DHY.Game.Admin.Dtos;

#region AI模型配置

/// <summary>
/// 更新模型配置输入
/// </summary>
public class UpdateModelConfigInput
{
    /// <summary>
    /// AI角色
    /// </summary>
    public string AiRole { get; set; }

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 温度参数
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// 是否启用思考模式
    /// </summary>
    public bool EnableThinking { get; set; }
}

/// <summary>
/// 测试AI连通性输入
/// </summary>
public class TestConnectionInput
{
    /// <summary>
    /// AI角色
    /// </summary>
    public string AiRole { get; set; }
}

/// <summary>
/// 模型配置输出
/// </summary>
public class ModelConfigOutput
{
    /// <summary>
    /// AI角色
    /// </summary>
    public string AiRole { get; set; }

    /// <summary>
    /// 模型ID
    /// </summary>
    public string ModelId { get; set; }

    /// <summary>
    /// 温度参数
    /// </summary>
    public double Temperature { get; set; }

    /// <summary>
    /// 是否启用思考模式
    /// </summary>
    public bool EnableThinking { get; set; }

    /// <summary>
    /// API基础URL
    /// </summary>
    public string BaseUrl { get; set; }

    /// <summary>
    /// API密钥（脱敏）
    /// </summary>
    public string ApiKeyMasked { get; set; }
}

/// <summary>
/// 连接测试结果
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 延迟(ms)
    /// </summary>
    public int LatencyMs { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

#endregion

#region Poixe测试

/// <summary>
/// Poixe模型测试输入
/// </summary>
public class TestPoixeInput
{
    /// <summary>用户输入的文本</summary>
    public string Prompt { get; set; } = "Hello";

    /// <summary>模型名称（如 grok-4-latest）</summary>
    public string? ModelId { get; set; }

    /// <summary>温度参数</summary>
    public double? Temperature { get; set; }

    /// <summary>是否启用思考模式</summary>
    public bool? EnableThinking { get; set; }

    /// <summary>是否流式输出</summary>
    public bool Stream { get; set; } = true;
}

/// <summary>
/// Poixe模型测试结果
/// </summary>
public class TestPoixeResult
{
    /// <summary>是否成功</summary>
    public bool IsSuccess { get; set; }

    /// <summary>HTTP状态码</summary>
    public int? StatusCode { get; set; }

    /// <summary>延迟(ms)</summary>
    public int LatencyMs { get; set; }

    /// <summary>发送的请求体JSON</summary>
    public string? RequestBody { get; set; }

    /// <summary>收到的原始响应体</summary>
    public string? ResponseBody { get; set; }

    /// <summary>解析后的AI回复文本</summary>
    public string? AiReply { get; set; }

    /// <summary>思考/推理过程内容（流式模式下从reasoning_content解析）</summary>
    public string? ThinkingContent { get; set; }

    /// <summary>错误信息</summary>
    public string? ErrorMessage { get; set; }
}

#endregion

#region 副本模板

/// <summary>
/// 分页查询副本模板输入
/// </summary>
public class TemplateListQueryInput
{
    /// <summary>页码</summary>
    public int PageIndex { get; set; }
    /// <summary>每页条数</summary>
    public int PageSize { get; set; }
    /// <summary>搜索关键词</summary>
    public string? Keyword { get; set; }
}

/// <summary>
/// 创建副本模板输入
/// </summary>
public class CreateTemplateInput
{
    /// <summary>
    /// 副本名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 世界观主题
    /// </summary>
    public string WorldTheme { get; set; }

    /// <summary>
    /// 难度等级 (E/D/C/B/A)
    /// </summary>
    public string Difficulty { get; set; }

    /// <summary>
    /// 时间限制天数
    /// </summary>
    public int TimeLimitDays { get; set; }

    /// <summary>
    /// 关键词标签
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// 副本描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 基础Prompt
    /// </summary>
    public string? BasePrompt { get; set; }

    /// <summary>
    /// 副本内升级上限
    /// </summary>
    public int MaxLevel { get; set; }

    /// <summary>
    /// 世界难度修正值（E=-3/D=-2/C=0/B=+2/A=+3）
    /// </summary>
    public int DifficultyModifier { get; set; }
}

/// <summary>
/// 更新副本模板输入
/// </summary>
public class UpdateTemplateInput : CreateTemplateInput
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public long Id { get; set; }
}

/// <summary>
/// 查询模板详情输入
/// </summary>
public class TemplateDetailQueryInput
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public long Id { get; set; }
}

/// <summary>
/// 删除模板输入
/// </summary>
public class DeleteTemplateInput
{
    /// <summary>
    /// 模板ID
    /// </summary>
    public long Id { get; set; }
}

/// <summary>
/// 难度统计输出
/// </summary>
public class DifficultyStatsOutput
{
    /// <summary>
    /// 难度等级
    /// </summary>
    public string Difficulty { get; set; }

    /// <summary>
    /// 数量
    /// </summary>
    public int Count { get; set; }
}

#endregion

#region 游戏参数

/// <summary>
/// 更新游戏选项输入
/// </summary>
public class UpdateGameOptionsInput
{
    public int? MaxBaseHp { get; set; }
    public int? HpPerConModifier { get; set; }
    public int? TimeSegmentsPerDay { get; set; }
    public int? OvertimePenalty { get; set; }
    public int? WoundThresholdPercent { get; set; }
    public int? RepositionInterval { get; set; }
    public int? MaxExpertiseSlots { get; set; }
    public int? MaxDungeonLevel { get; set; }
}

/// <summary>
/// 游戏AI配置输出(隐藏ApiKey)
/// </summary>
public class GameAiOptionsOutput
{
    public Dictionary<string, ModelConfigOutput> Models { get; set; }
    public int TimeoutSeconds { get; set; }
    public int MaxRetries { get; set; }
}

#endregion

#region 游戏监控

/// <summary>
/// 会话详情查询输入
/// </summary>
public class SessionDetailQueryInput
{
    /// <summary>会话ID</summary>
    public long SessionId { get; set; }
}

/// <summary>
/// 日统计查询输入
/// </summary>
public class DailyStatsQueryInput
{
    /// <summary>统计日期</summary>
    public DateTime Date { get; set; }
}

/// <summary>
/// 活跃会话输出
/// </summary>
public class ActiveSessionOutput
{
    public long SessionId { get; set; }
    public long UserId { get; set; }
    public string? DungeonName { get; set; }
    public DateTime StartTime { get; set; }
    public int InteractionCount { get; set; }
    public int Status { get; set; }
}

/// <summary>
/// 日统计输出
/// </summary>
public class DailyStatsOutput
{
    public int NewSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int AbandonedSessions { get; set; }
    public double AvgDurationMinutes { get; set; }
    public double AvgInteractions { get; set; }
}

/// <summary>
/// 总览输出
/// </summary>
public class OverviewOutput
{
    public int TotalUsers { get; set; }
    public int TotalSessions { get; set; }
    public int ActiveSessions { get; set; }
    public long TotalAiCalls { get; set; }
    public long TotalTokens { get; set; }
}

#endregion

#region Token统计

/// <summary>
/// 日期范围查询输入
/// </summary>
public class DateRangeQueryInput
{
    /// <summary>开始日期</summary>
    public DateTime StartDate { get; set; }
    /// <summary>结束日期</summary>
    public DateTime EndDate { get; set; }
}

/// <summary>
/// 趋势查询输入
/// </summary>
public class TrendQueryInput
{
    /// <summary>查询天数(默认7)</summary>
    public int Days { get; set; } = 7;
}

/// <summary>
/// Token使用汇总输出
/// </summary>
public class TokenUsageSummaryOutput
{
    public long TotalInputTokens { get; set; }
    public long TotalOutputTokens { get; set; }
    public decimal TotalCost { get; set; }
    public List<ModelUsageItem> ByModel { get; set; } = new();
}

/// <summary>
/// 模型使用项
/// </summary>
public class ModelUsageItem
{
    public string ModelName { get; set; }
    public int CallCount { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public decimal Cost { get; set; }
}

/// <summary>
/// 使用趋势项
/// </summary>
public class UsageTrendItem
{
    public DateTime Date { get; set; }
    public long TotalTokens { get; set; }
    public decimal Cost { get; set; }
    public int CallCount { get; set; }
}

/// <summary>
/// 错误率统计输出
/// </summary>
public class ErrorRateOutput
{
    public int TotalCalls { get; set; }
    public int FailedCalls { get; set; }
    public double ErrorRate { get; set; }
    public List<ErrorTypeItem> ByType { get; set; } = new();
}

/// <summary>
/// 错误类型项
/// </summary>
public class ErrorTypeItem
{
    public string ErrorType { get; set; }
    public int Count { get; set; }
}

/// <summary>
/// 费用预估输出
/// </summary>
public class CostEstimateOutput
{
    public decimal DailyAvgCost { get; set; }
    public decimal MonthlyEstimate { get; set; }
    public long DailyAvgTokens { get; set; }
}

#endregion
