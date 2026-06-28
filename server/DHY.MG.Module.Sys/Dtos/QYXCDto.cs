namespace DHY.MG.Module.Sys.Dtos
{
    /// <summary>
    /// 通义星尘新版API请求DTO (对应 /v2/api/chat/completions)
    /// 参考文档: https://help.aliyun.com/zh/document_detail/2861866.html
    /// </summary>
    public class QYXCDto
    {
        /// <summary>
        /// 模型名称,推荐使用xingchen-plus-latest
        /// </summary>
        public string model { get; set; } = "xingchen-plus-latest";

        /// <summary>
        /// 是否流式输出
        /// </summary>
        public bool stream { get; set; } = true;

        /// <summary>
        /// 对话历史消息列表(包含system、user、assistant角色)
        /// </summary>
        public IList<QYMessages> messages { get; set; }

        /// <summary>
        /// 温度值,较高的值将使输出更加随机
        /// </summary>
        public double? temperature { get; set; }

        /// <summary>
        /// 核采样方法概率阈值
        /// </summary>
        public double? top_p { get; set; }

        /// <summary>
        /// 用户唯一标识(用于session-cache场景)
        /// </summary>
        public string user { get; set; }
    }

    public class QYInput
    {
        public IList<QYMessages> messages { get; set; }

        public AcaRole aca { get; set; }
    }

    public class QYMessages
    {
        //public string name { get; set; }
        public string role { get; set; }
        public string content { get; set; }
    }

    public class AcaRole
    {
        public BotProfile botProfile { get; set; }

        public UserProfile userProfile { get; set; }

        public Scenario scenario { get; set; }

        //public IList<QYMessages> SampleMessages { get; set; }
    }

    public class BotProfile
    {
        public string name { get; set; }
        /// <summary>
        /// 【你的人设】
        /// </summary>
        public string content { get; set; }
        /// <summary>
        /// 【强制要求】
        /// </summary>
        public string traits { get; set; }

        public double LoveVal { get; set; } = 0;

    }

    public class UserProfile
    {
        public string userId { get; set; }
        public string userName { get; set; }
        public string basicInfo { get; set; }
    }

    public class Scenario
    {
        public string description { get; set; }

        public bool isRealTime { get; set; } = true;

    }

    public class Parameters
    {
        public bool incrementalOutput { get; set; }
    }
}
