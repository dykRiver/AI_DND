namespace DHY.MG.Module.Sys.Dtos
{
    #region API1: 会话列表识别

    /// <summary>
    /// 会话列表截图识别 - 请求
    /// </summary>
    public class ChatListRecognizeInput
    {
        /// <summary>
        /// 截图Base64编码（不含 data:image/...;base64, 前缀）
        /// </summary>
        public string ImageBase64 { get; set; }

        /// <summary>
        /// 图像格式，默认 png
        /// </summary>
        public string ImageFormat { get; set; } = "png";
    }

    /// <summary>
    /// 会话列表截图识别 - 响应
    /// </summary>
    public class ChatListRecognizeOutput
    {
        /// <summary>
        /// 识别到的会话列表
        /// </summary>
        public List<ChatListSessionItem> Sessions { get; set; } = new();

        /// <summary>
        /// 识别到的会话总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        public long ProcessTimeMs { get; set; }
    }

    /// <summary>
    /// 会话列表中的单个会话项
    /// </summary>
    public class ChatListSessionItem
    {
        /// <summary>
        /// 会话序号（从1开始）
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 会话名称（不含标签如[内部群]等）
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 时间文本（如 "09:22"、"昨天"、"02-11"）
        /// </summary>
        public string Time { get; set; }

        /// <summary>
        /// 会话中心点X坐标（相对于原始截图的像素坐标）
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// 会话中心点Y坐标（相对于原始截图的像素坐标）
        /// </summary>
        public int Y { get; set; }
    }

    #endregion

    #region API2: 消息重要性分析

    /// <summary>
    /// 消息重要性分析 - 请求
    /// </summary>
    public class MessageAnalyzeInput
    {
        /// <summary>
        /// 用户配置信息
        /// </summary>
        public DDBotUserProfile UserProfile { get; set; }

        /// <summary>
        /// 待分析的消息列表
        /// </summary>
        public List<DDBotMessageItem> Messages { get; set; } = new();

        /// <summary>
        /// 会话名称
        /// </summary>
        public string ConversationName { get; set; }

        /// <summary>
        /// 会话类型: "group" / "private"
        /// </summary>
        public string ConversationType { get; set; } = "group";
    }

    /// <summary>
    /// 用户配置信息
    /// </summary>
    public class DDBotUserProfile
    {
        /// <summary>
        /// 用户姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 别名列表
        /// </summary>
        public List<string> Aliases { get; set; } = new();

        /// <summary>
        /// 职位
        /// </summary>
        public string Role { get; set; }

        /// <summary>
        /// 关注项目列表
        /// </summary>
        public List<DDBotProject> Projects { get; set; } = new();

        /// <summary>
        /// 关键词配置
        /// </summary>
        public DDBotKeywords Keywords { get; set; } = new();

        /// <summary>
        /// @我的消息是否一定标记为urgent
        /// </summary>
        public bool AtMeAlwaysUrgent { get; set; } = true;

        /// <summary>
        /// @所有人的消息是否一定标记为important
        /// </summary>
        public bool AtAllAlwaysImportant { get; set; } = true;

        /// <summary>
        /// 私聊消息是否一定标记为important
        /// </summary>
        public bool PrivateAlwaysImportant { get; set; } = true;

        /// <summary>
        /// 关注事项描述（自由文本，用于AI分析上下文）
        /// </summary>
        public string FocusDescription { get; set; }
    }

    /// <summary>
    /// 关注项目
    /// </summary>
    public class DDBotProject
    {
        /// <summary>
        /// 项目名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 项目关注描述（自由文本，供AI综合分析使用）
        /// </summary>
        public string Focus { get; set; }

        /// <summary>
        /// 项目关键词（用于规则预筛匹配）
        /// </summary>
        public List<string> Keywords { get; set; } = new();
    }

    /// <summary>
    /// 关键词配置
    /// </summary>
    public class DDBotKeywords
    {
        /// <summary>
        /// 紧急关键词列表
        /// </summary>
        public List<string> Urgent { get; set; } = new();

        /// <summary>
        /// 重要关键词列表
        /// </summary>
        public List<string> Important { get; set; } = new();
    }

    /// <summary>
    /// 待分析的消息项
    /// </summary>
    public class DDBotMessageItem
    {
        /// <summary>
        /// 消息编号（批量中的序号，从1开始）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 发送者名称
        /// </summary>
        public string Sender { get; set; }

        /// <summary>
        /// 消息内容
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// 消息时间文本
        /// </summary>
        public string MsgTime { get; set; }

        /// <summary>
        /// 消息类型: "text" / "image" / "file" / "system"
        /// </summary>
        public string MsgType { get; set; } = "text";

        /// <summary>
        /// 消息指纹（客户端生成的唯一标识）
        /// </summary>
        public string Fingerprint { get; set; }

        /// <summary>
        /// 是否为回复消息
        /// </summary>
        public bool IsReply { get; set; }

        /// <summary>
        /// 被引用消息的发送者
        /// </summary>
        public string QuotedSender { get; set; }

        /// <summary>
        /// 被引用消息的内容
        /// </summary>
        public string QuotedContent { get; set; }
    }

    /// <summary>
    /// 消息重要性分析 - 响应
    /// </summary>
    public class MessageAnalyzeOutput
    {
        /// <summary>
        /// 分析结果列表
        /// </summary>
        public List<MessageAnalysisResultItem> Results { get; set; } = new();

        /// <summary>
        /// 消息总数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 紧急消息数
        /// </summary>
        public int UrgentCount { get; set; }

        /// <summary>
        /// 重要消息数
        /// </summary>
        public int ImportantCount { get; set; }

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        public long ProcessTimeMs { get; set; }
    }

    /// <summary>
    /// 单条消息分析结果
    /// </summary>
    public class MessageAnalysisResultItem
    {
        /// <summary>
        /// 消息编号（对应请求中的Id）
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 消息指纹
        /// </summary>
        public string Fingerprint { get; set; }

        /// <summary>
        /// 重要程度: "urgent" / "important" / "normal" / "ignore"
        /// </summary>
        public string Level { get; set; }

        /// <summary>
        /// 判断原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 判断方法: "rule" / "ai"
        /// </summary>
        public string Method { get; set; }
    }

    #endregion
}
