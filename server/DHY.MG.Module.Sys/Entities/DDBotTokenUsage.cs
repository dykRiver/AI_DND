namespace DHY.MG.Module.Sys.Entities;

/// <summary>
/// DDBot Token使用明细记录表
/// 记录每次AI调用的详细token消耗
/// </summary>
[SugarTable("DDBot_TokenUsage_Detail", "DDBot Token使用明细表")]
public class DDBotTokenUsageDetail : EntityBase
{
    /// <summary>
    /// 调用日期
    /// </summary>
    [SugarColumn(ColumnDescription = "调用日期")]
    public DateTime CallDate { get; set; }

    /// <summary>
    /// 调用时间(精确到小时)
    /// </summary>
    [SugarColumn(ColumnDescription = "调用小时")]
    public int CallHour { get; set; }

    /// <summary>
    /// 模型名称(如 qwen-turbo, qwen-vl-ocr等)
    /// </summary>
    [SugarColumn(ColumnDescription = "模型名称", Length = 64)]
    public string ModelName { get; set; }

    /// <summary>
    /// API类型(recognize=会话列表识别, analyze=消息分析)
    /// </summary>
    [SugarColumn(ColumnDescription = "API类型", Length = 32)]
    public string ApiType { get; set; }

    /// <summary>
    /// 输入token数(prompt tokens)
    /// </summary>
    [SugarColumn(ColumnDescription = "输入Token数")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// 输出token数(completion tokens)
    /// </summary>
    [SugarColumn(ColumnDescription = "输出Token数")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// 总token数
    /// </summary>
    [SugarColumn(ColumnDescription = "总Token数")]
    public int TotalTokens { get; set; }

    /// <summary>
    /// 会话名称(可选,消息分析时有值)
    /// </summary>
    [SugarColumn(ColumnDescription = "会话名称", Length = 256, IsNullable = true)]
    public string? ConversationName { get; set; }

    /// <summary>
    /// 用户ID(从JWT Token中获取)
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", Length = 64, IsNullable = true)]
    public string? UserId { get; set; }

    /// <summary>
    /// 用户账号(从JWT Token中获取)
    /// </summary>
    [SugarColumn(ColumnDescription = "用户账号", Length = 64, IsNullable = true)]
    public string? UserAccount { get; set; }

    /// <summary>
    /// 客户端类型(从请求头X-Client-Type中获取,如collector/reminder)
    /// </summary>
    [SugarColumn(ColumnDescription = "客户端类型", Length = 64, IsNullable = true)]
    public string? ClientType { get; set; }

    /// <summary>
    /// 调用耗时(毫秒)
    /// </summary>
    [SugarColumn(ColumnDescription = "调用耗时(毫秒)")]
    public long ProcessTimeMs { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功")]
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 错误信息(失败时记录)
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 512, IsNullable = true)]
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DDBot Token使用统计表(按天/小时聚合)
/// </summary>
[SugarTable("DDBot_TokenUsage_Stats", "DDBot Token使用统计表")]
public class DDBotTokenUsageStats : EntityBase
{
    /// <summary>
    /// 统计日期
    /// </summary>
    [SugarColumn(ColumnDescription = "统计日期")]
    public DateTime StatsDate { get; set; }

    /// <summary>
    /// 统计小时(0-23, 0表示全天汇总)
    /// </summary>
    [SugarColumn(ColumnDescription = "统计小时")]
    public int StatsHour { get; set; } = 0;

    /// <summary>
    /// 模型名称
    /// </summary>
    [SugarColumn(ColumnDescription = "模型名称", Length = 64)]
    public string ModelName { get; set; }

    /// <summary>
    /// API类型
    /// </summary>
    [SugarColumn(ColumnDescription = "API类型", Length = 32)]
    public string ApiType { get; set; }

    /// <summary>
    /// 调用次数
    /// </summary>
    [SugarColumn(ColumnDescription = "调用次数")]
    public int CallCount { get; set; }

    /// <summary>
    /// 成功次数
    /// </summary>
    [SugarColumn(ColumnDescription = "成功次数")]
    public int SuccessCount { get; set; }

    /// <summary>
    /// 失败次数
    /// </summary>
    [SugarColumn(ColumnDescription = "失败次数")]
    public int FailedCount { get; set; }

    /// <summary>
    /// 总输入token数
    /// </summary>
    [SugarColumn(ColumnDescription = "总输入Token数")]
    public long TotalPromptTokens { get; set; }

    /// <summary>
    /// 总输出token数
    /// </summary>
    [SugarColumn(ColumnDescription = "总输出Token数")]
    public long TotalCompletionTokens { get; set; }

    /// <summary>
    /// 总token数
    /// </summary>
    [SugarColumn(ColumnDescription = "总Token数")]
    public long TotalTokens { get; set; }

    /// <summary>
    /// 平均耗时(毫秒)
    /// </summary>
    [SugarColumn(ColumnDescription = "平均耗时(毫秒)")]
    public long AvgProcessTimeMs { get; set; }

    /// <summary>
    /// 预估成本(元)
    /// </summary>
    [SugarColumn(ColumnDescription = "预估成本(元)")]
    public decimal EstimatedCost { get; set; }
}

/// <summary>
/// DDBot AI模型单价配置表
/// </summary>
[SugarTable("DDBot_Model_Price", "DDBot AI模型单价配置表")]
public class DDBotModelPrice : EntityBase
{
    /// <summary>
    /// 模型名称
    /// </summary>
    [SugarColumn(ColumnDescription = "模型名称", Length = 64)]
    public string ModelName { get; set; }

    /// <summary>
    /// 模型显示名称
    /// </summary>
    [SugarColumn(ColumnDescription = "模型显示名称", Length = 128)]
    public string DisplayName { get; set; }

    /// <summary>
    /// 输入单价(元/千token)
    /// </summary>
    [SugarColumn(ColumnDescription = "输入单价(元/千token)")]
    public decimal InputPricePerThousand { get; set; }

    /// <summary>
    /// 输出单价(元/千token)
    /// </summary>
    [SugarColumn(ColumnDescription = "输出单价(元/千token)")]
    public decimal OutputPricePerThousand { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(ColumnDescription = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 256, IsNullable = true)]
    public string? Remark { get; set; }
}
