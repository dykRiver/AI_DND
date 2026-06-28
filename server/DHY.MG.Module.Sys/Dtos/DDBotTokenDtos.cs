namespace DHY.MG.Module.Sys.Dtos
{
    #region Token统计查询相关DTO

    /// <summary>
    /// Token统计查询请求
    /// </summary>
    public class DDBotTokenStatsQueryInput
    {
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 统计粒度: "hour"=按小时, "day"=按天
        /// </summary>
        public string Granularity { get; set; } = "day";

        /// <summary>
        /// 模型名称(可选,筛选特定模型)
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// API类型(可选,筛选特定接口)
        /// </summary>
        public string? ApiType { get; set; }
    }

    /// <summary>
    /// Token统计响应
    /// </summary>
    public class DDBotTokenStatsOutput
    {
        /// <summary>
        /// 统计数据列表
        /// </summary>
        public List<DDBotTokenStatsItem> Data { get; set; } = new();

        /// <summary>
        /// 汇总信息
        /// </summary>
        public DDBotTokenSummary Summary { get; set; } = new();
    }

    /// <summary>
    /// 单条统计项
    /// </summary>
    public class DDBotTokenStatsItem
    {
        /// <summary>
        /// 日期时间
        /// </summary>
        public DateTime DateTime { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// API类型
        /// </summary>
        public string ApiType { get; set; }

        /// <summary>
        /// 调用次数
        /// </summary>
        public int CallCount { get; set; }

        /// <summary>
        /// 总token数
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// 输入token数
        /// </summary>
        public long PromptTokens { get; set; }

        /// <summary>
        /// 输出token数
        /// </summary>
        public long CompletionTokens { get; set; }

        /// <summary>
        /// 预估成本(元)
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// 平均耗时(毫秒)
        /// </summary>
        public long AvgTimeMs { get; set; }
    }

    /// <summary>
    /// Token汇总信息
    /// </summary>
    public class DDBotTokenSummary
    {
        /// <summary>
        /// 总调用次数
        /// </summary>
        public int TotalCalls { get; set; }

        /// <summary>
        /// 总token数
        /// </summary>
        public long TotalTokens { get; set; }

        /// <summary>
        /// 总输入token
        /// </summary>
        public long TotalPromptTokens { get; set; }

        /// <summary>
        /// 总输出token
        /// </summary>
        public long TotalCompletionTokens { get; set; }

        /// <summary>
        /// 总成本(元)
        /// </summary>
        public decimal TotalCost { get; set; }

        /// <summary>
        /// 成功次数
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// 失败次数
        /// </summary>
        public int FailedCount { get; set; }

        /// <summary>
        /// 成功率
        /// </summary>
        public double SuccessRate { get; set; }
    }

    /// <summary>
    /// Token使用明细查询请求
    /// </summary>
    public class DDBotTokenDetailQueryInput : BasePageInput
    {
        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 模型名称(可选)
        /// </summary>
        public string? ModelName { get; set; }

        /// <summary>
        /// API类型(可选)
        /// </summary>
        public string? ApiType { get; set; }

        /// <summary>
        /// 是否成功(可选)
        /// </summary>
        public bool? IsSuccess { get; set; }
    }

    /// <summary>
    /// Token使用明细响应
    /// </summary>
    public class DDBotTokenDetailOutput
    {
        /// <summary>
        /// 调用日期时间
        /// </summary>
        public DateTime CallTime { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// API类型
        /// </summary>
        public string ApiType { get; set; }

        /// <summary>
        /// 输入token数
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// 输出token数
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// 总token数
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// 会话名称
        /// </summary>
        public string? ConversationName { get; set; }

        /// <summary>
        /// 调用耗时(毫秒)
        /// </summary>
        public long ProcessTimeMs { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    #endregion

    #region Token记录相关DTO

    /// <summary>
    /// 记录Token使用请求
    /// </summary>
    public class RecordTokenUsageInput
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// API类型(如: recognize/analyze)
        /// </summary>
        public string ApiType { get; set; }

        /// <summary>
        /// 输入token数
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// 输出token数
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// 总token数
        /// </summary>
        public int TotalTokens { get; set; }

        /// <summary>
        /// 处理耗时(毫秒)
        /// </summary>
        public long ProcessTimeMs { get; set; }

        /// <summary>
        /// 是否成功(默认true)
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// 错误信息(失败时填写)
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// 会话名称(可选)
        /// </summary>
        public string? ConversationName { get; set; }
    }

    #endregion

    #region 模型单价配置相关DTO

    /// <summary>
    /// 模型单价配置输入
    /// </summary>
    public class DDBotModelPriceInput
    {
        /// <summary>
        /// ID(更新时必填)
        /// </summary>
        public long? Id { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 模型显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 输入单价(元/千token)
        /// </summary>
        public decimal InputPricePerThousand { get; set; }

        /// <summary>
        /// 输出单价(元/千token)
        /// </summary>
        public decimal OutputPricePerThousand { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 模型单价配置输出
    /// </summary>
    public class DDBotModelPriceOutput
    {
        /// <summary>
        /// ID
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// 模型显示名称
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 输入单价(元/千token)
        /// </summary>
        public decimal InputPricePerThousand { get; set; }

        /// <summary>
        /// 输出单价(元/千token)
        /// </summary>
        public decimal OutputPricePerThousand { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime? CreatedTime { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdatedTime { get; set; }
    }

    #endregion
}
