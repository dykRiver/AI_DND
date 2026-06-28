namespace DHY.MG.Module.Sys.Dtos
{
    /// <summary>
    /// 通义星尘新版API响应DTO
    /// 参考文档: https://help.aliyun.com/zh/document_detail/2861866.html
    /// </summary>
    public class QYXCResultDto
    {
        /// <summary>
        /// 请求唯一标识
        /// </summary>
        public string id { get; set; }

        /// <summary>
        /// 对象类型 (json:chat.completion 或 sse:chat.completion.chunk)
        /// </summary>
        public string @object { get; set; }

        /// <summary>
        /// 时间戳(秒)
        /// </summary>
        public long created { get; set; }

        /// <summary>
        /// 模型名称
        /// </summary>
        public string model { get; set; }

        /// <summary>
        /// 回复结果列表
        /// </summary>
        public IList<Choice> choices { get; set; }

        /// <summary>
        /// 计量token信息
        /// </summary>
        public QYUsage usage { get; set; }

        /// <summary>
        /// 回复结束标记(旧版字段,保留兼容)
        /// </summary>
        public bool stop { get; set; }
    }

    public class Choice
    {
        /// <summary>
        /// n回复索引
        /// </summary>
        public int index { get; set; }

        /// <summary>
        /// 流式增量输出的消息内容(新版API使用delta而非message)
        /// </summary>
        public ChoiceMes delta { get; set; }

        /// <summary>
        /// 非流式输出的消息内容(新版API中仅非流式时使用)
        /// </summary>
        public ChoiceMes message { get; set; }

        /// <summary>
        /// 回复结束类型: null(生成中)、stop(生成结束)、length(达到最大长度)
        /// </summary>
        public string finish_reason { get; set; }

        /// <summary>
        /// 旧版字段,保留兼容
        /// </summary>
        public IList<ChoiceMes> messages { get; set; }

        /// <summary>
        /// 旧版字段,保留兼容
        /// </summary>
        public string stopReason { get; set; }
    }

    public class ChoiceMes
    {
        /// <summary>
        /// 消息角色: system/user/assistant
        /// </summary>
        public string role { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string content { get; set; }

        /// <summary>
        /// 旧版字段,保留兼容
        /// </summary>
        public string finishReason { get; set; }

        /// <summary>
        /// 旧版字段,保留兼容
        /// </summary>
        public string validMessage { get; set; }

        /// <summary>
        /// 旧版字段,保留兼容
        /// </summary>
        public string functionMessage { get; set; }
    }

    public class QYUsage
    {
        /// <summary>
        /// 输入Token数量
        /// </summary>
        public int prompt_tokens { get; set; }

        /// <summary>
        /// 输出Token数量
        /// </summary>
        public int completion_tokens { get; set; }

        /// <summary>
        /// 总Token数量
        /// </summary>
        public int total_tokens { get; set; }

        /// <summary>
        /// 旧版字段,保留兼容
        /// </summary>
        public int outputTokens { get; set; }

        /// <summary>
        /// 旧版字段,保留兼容
        /// </summary>
        public int inputTokens { get; set; }
    }
}
