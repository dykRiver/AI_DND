using Furion.ConfigurableOptions;

namespace DHY.MG.Module.Sys.Dtos
{
    /// <summary>
    /// 海龟汤渐进式线索配置选项
    /// </summary>
    public class GradualClueOptions : IConfigurableOptions
    {
        /// <summary>
        /// 是否启用渐进式线索功能
        /// true=启用（默认），玩家在95分以下时不显示完整答案
        /// false=禁用，恢复原有的一次性揭示模式
        /// </summary>
        public bool EnableGradualClue { get; set; } = true;

        /// <summary>
        /// 显示完整答案的分数阈值（默认95）
        /// 达到或超过此分数时，才显示完整标准答案
        /// </summary>
        public int FullAnswerThreshold { get; set; } = 95;

        /// <summary>
        /// 是否启用AI动态生成线索
        /// true=调用AI模型生成个性化线索（默认）
        /// false=使用预设线索模板
        /// </summary>
        public bool EnableAIClueGeneration { get; set; } = true;

        /// <summary>
        /// 线索缓存配置
        /// </summary>
        public ClueCacheOptions ClueCache { get; set; } = new ClueCacheOptions();

        /// <summary>
        /// 分数等级配置列表
        /// 定义每个分数段的名称、激励话术和揭示策略
        /// </summary>
        public List<ScoreLevelConfig> ScoreLevels { get; set; } = new List<ScoreLevelConfig>();

        /// <summary>
        /// 预设线索模板字典（后备方案）
        /// 当AI调用失败时使用，按分数段提供后备线索内容
        /// Key格式: "0-39", "40-59", "60-79", "80-89", "90-94"
        /// </summary>
        public Dictionary<string, string> FallbackClues { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// 线索缓存配置选项
    /// </summary>
    public class ClueCacheOptions
    {
        /// <summary>
        /// 是否启用线索缓存
        /// true=相同分数段的线索会缓存指定时长（默认）
        /// false=每次都重新生成
        /// </summary>
        public bool EnableCache { get; set; } = true;

        /// <summary>
        /// 缓存时长（小时）
        /// 默认24小时，超时后自动清除
        /// </summary>
        public int CacheDuration { get; set; } = 24;

        /// <summary>
        /// 缓存过期时间（分钟），从CacheDuration（小时）转换得出
        /// </summary>
        public int CacheExpirationMinutes => CacheDuration * 60;
    }

    /// <summary>
    /// 分数等级配置
    /// </summary>
    public class ScoreLevelConfig
    {
        /// <summary>
        /// 最低分数（包含）
        /// </summary>
        public int MinScore { get; set; }

        /// <summary>
        /// 最高分数（包含）
        /// </summary>
        public int MaxScore { get; set; }

        /// <summary>
        /// 等级名称（如：初级、中级、高级、完美）
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// 等级标识（Level属性映射到LevelName）
        /// </summary>
        public string Level => LevelName;

        /// <summary>
        /// 揭示百分比（0-100）
        /// 表示该分数段应揭示多少百分比的谜底信息
        /// </summary>
        public int RevealPercentage { get; set; }

        /// <summary>
        /// 线索比例（0.0-1.0），从RevealPercentage计算得出
        /// </summary>
        public double ClueRatio => RevealPercentage / 100.0;

        /// <summary>
        /// 激励话术模板
        /// </summary>
        public string EncouragementTemplate { get; set; }

        /// <summary>
        /// 鼓励话术（Encouragement属性映射到EncouragementTemplate）
        /// </summary>
        public string Encouragement => EncouragementTemplate;

        /// <summary>
        /// 反馈文字（使用激励话术作为反馈）
        /// </summary>
        public string Feedback => EncouragementTemplate;
    }

    /// <summary>
    /// 分级线索数据传输对象
    /// 用于内部线索生成逻辑的数据传递
    /// </summary>
    public class GradualClueDto
    {
        /// <summary>
        /// 分数等级
        /// </summary>
        public string ScoreLevel { get; set; }

        /// <summary>
        /// 反馈文字
        /// </summary>
        public string Feedback { get; set; }

        /// <summary>
        /// 线索类型
        /// </summary>
        public string ClueType { get; set; }

        /// <summary>
        /// 部分线索内容
        /// </summary>
        public string PartialClue { get; set; }

        /// <summary>
        /// 线索内容（ClueContent属性映射到PartialClue）
        /// </summary>
        public string ClueContent
        {
            get => PartialClue;
            set => PartialClue = value;
        }

        /// <summary>
        /// 完整答案
        /// </summary>
        public string CorrectAnswer { get; set; }

        /// <summary>
        /// 激励话术
        /// </summary>
        public string EncouragementText { get; set; }

        /// <summary>
        /// 是否显示完整答案
        /// </summary>
        public bool ShouldShowFullAnswer { get; set; }

        /// <summary>
        /// 是否命中缓存
        /// </summary>
        public bool CacheHit { get; set; }
    }

    /// <summary>
    /// AI评分和线索生成结果（合并功能）
    /// 用于一次性返回评分和线索的数据传输对象
    /// </summary>
    public class AIScoreAndClueResult
    {
        /// <summary>
        /// 评分（0-100）
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// 分数等级标签
        /// </summary>
        public string ScoreLevel { get; set; }

        /// <summary>
        /// 反馈文字
        /// </summary>
        public string Feedback { get; set; }

        /// <summary>
        /// 线索类型
        /// </summary>
        public string ClueType { get; set; }

        /// <summary>
        /// 线索内容
        /// </summary>
        public string ClueContent { get; set; }

        /// <summary>
        /// 激励话术
        /// </summary>
        public string EncouragementText { get; set; }

        /// <summary>
        /// 是否命中缓存
        /// </summary>
        public bool CacheHit { get; set; }
    }
}
