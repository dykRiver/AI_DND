using Furion.ConfigurableOptions;

namespace DHY.MG.Module.Sys.Dtos
{
    /// <summary>
    /// DDBot 钉钉消息分析服务配置选项
    /// </summary>
    public class DDBotOptions : IConfigurableOptions
    {
        #region AI模型配置

        /// <summary>
        /// 是否启用双阶段识别模式（先用便宜模型，失败再用强大模型）
        /// </summary>
        public bool EnableTwoStageRecognition { get; set; } = true;

        /// <summary>
        /// 消息分析模型（用于判断消息重要性）
        /// </summary>
        public string AnalysisModel { get; set; } = "qwen3.5-plus";

        /// <summary>
        /// 消息分析温度参数（越低越确定性）
        /// </summary>
        public double AnalysisTemperature { get; set; } = 0.1;

        /// <summary>
        /// 消息分析最大token数
        /// </summary>
        public int AnalysisMaxTokens { get; set; } = 2000;

        /// <summary>
        /// 消息分析是否启用思考模式
        /// 开启后AI先推理再输出，判断更准确但响应更慢
        /// 仅支持 qwen3.5-plus、qwen3-max 等混合思考模型，且强制使用流式输出
        /// </summary>
        public bool AnalysisEnableThinking { get; set; } = false;

        /// <summary>
        /// 消息分析思考预算Token数（默认3000）
        /// 仅在 AnalysisEnableThinking=true 时生效，建议范围：1000-8000
        /// </summary>
        public int AnalysisThinkingBudget { get; set; } = 3000;

        /// <summary>
        /// 第一阶段OCR识别最大token数（双阶段模式下使用）
        /// </summary>
        public int FirstStageOcrMaxTokens { get; set; } = 8192;

        /// <summary>
        /// 第二阶段OCR识别最大token数（双阶段模式下使用）
        /// </summary>
        public int SecondStageOcrMaxTokens { get; set; } = 3000;

        /// <summary>
        /// 第一阶段OCR视觉识别模型（双阶段模式下使用）
        /// </summary>
        public string FirstStageOcrModel { get; set; } = "qwen-vl-ocr-latest";

        /// <summary>
        /// 第二阶段OCR视觉识别模型（双阶段模式下使用）
        /// </summary>
        public string SecondStageOcrModel { get; set; } = "qwen3.5-plus";

        /// <summary>
        /// 第一阶段OCR是否启用思考模式（双阶段模式下使用）
        /// </summary>
        public bool FirstStageOcrEnableThinking { get; set; } = false;

        /// <summary>
        /// 第二阶段OCR是否启用思考模式（双阶段模式下使用）
        /// </summary>
        public bool SecondStageOcrEnableThinking { get; set; } = true;

        /// <summary>
        /// API调用失败重试次数
        /// </summary>
        public int MaxRetries { get; set; } = 2;

        #endregion

        #region 图像裁剪配置

        /// <summary>
        /// 左侧裁剪像素（去除头像区域）
        /// </summary>
        public int CropLeft { get; set; } = 80;

        /// <summary>
        /// 顶部裁剪像素（去除导航栏）
        /// </summary>
        public int CropTop { get; set; } = 50;

        /// <summary>
        /// 右侧裁剪像素
        /// </summary>
        public int CropRight { get; set; } = 0;

        /// <summary>
        /// 底部固定裁剪像素
        /// </summary>
        public int CropBottom { get; set; } = 0;

        /// <summary>
        /// 目标裁剪后高度（>0时动态计算底部裁剪）
        /// </summary>
        public int CropTargetHeight { get; set; } = 0;

        /// <summary>
        /// 最大图像宽度（超过则等比缩放）
        /// </summary>
        public int MaxImageWidth { get; set; } = 1280;

        #endregion

        #region 图像涂白配置

        /// <summary>
        /// 是否涂白预览消息区域
        /// </summary>
        public bool MaskPreview { get; set; } = true;

        /// <summary>
        /// 涂白区域高度（像素）
        /// </summary>
        public int MaskHeight { get; set; } = 28;

        /// <summary>
        /// 涂白区域相对于会话名称中心的垂直偏移（向下为正）
        /// </summary>
        public int MaskOffsetY { get; set; } = 15;

        /// <summary>
        /// 涂白区域左边距
        /// </summary>
        public int MaskMarginLeft { get; set; } = 0;

        /// <summary>
        /// 涂白区域右边距
        /// </summary>
        public int MaskMarginRight { get; set; } = 80;

        /// <summary>
        /// 是否在固定位置绘制红线（用于调试参考线）
        /// </summary>
        public bool DrawFixedRedLine { get; set; } = false;

        /// <summary>
        /// 固定红线的Y坐标位置（相对于裁剪后图像的像素位置）
        /// </summary>
        public int FixedRedLineY { get; set; } = 100;

        #endregion

        #region 固定布局坐标配置

        /// <summary>
        /// 第一个会话中心X坐标（相对于原始截图）
        /// </summary>
        public int LayoutFirstX { get; set; } = 167;

        /// <summary>
        /// 第一个会话中心Y坐标（相对于原始截图）
        /// </summary>
        public int LayoutFirstY { get; set; } = 78;

        /// <summary>
        /// 相邻会话的垂直间距（像素）
        /// </summary>
        public int LayoutSpacing { get; set; } = 80;

        #endregion

        #region 批量分析配置

        /// <summary>
        /// 每批送AI分析的消息数
        /// </summary>
        public int BatchSize { get; set; } = 10;

        /// <summary>
        /// 是否启用规则预筛（在AI分析前先用关键词等规则过滤）
        /// </summary>
        public bool EnableRulePreFilter { get; set; } = true;

        #endregion

        /// <summary>
        /// 是否启用调试日志
        /// </summary>
        public bool EnableDebugLog { get; set; } = false;

        /// <summary>
        /// 是否记录完整的AI请求参数（包括提示词内容）
        /// </summary>
        public bool LogFullAiRequests { get; set; } = false;

        /// <summary>
        /// 是否记录完整的AI响应内容
        /// </summary>
        public bool LogFullAiResponses { get; set; } = false;

        /// <summary>
        /// 是否将AI完整响应保存到独立日志文件
        /// </summary>
        public bool SaveFullAiResponseToFile { get; set; } = false;

        /// <summary>
        /// AI响应日志文件保存目录
        /// </summary>
        public string AiResponseLogDir { get; set; } = "ai_logs";


        #region 调试图片保存配置

        /// <summary>
        /// 是否保存预处理后的调试图片(默认false)
        /// true:每次识别请求后,将裁剪+涂白+缩放处理结果保存为 PNG 文件,便于查看处理效果
        /// false:不保存,仅在内存中处理
        /// </summary>
        public bool SaveDebugImage { get; set; } = true;

        /// <summary>
        /// 调试图片保存目录(默认为空,表示使用应用程序当前目录下的 debug_images 子目录)
        /// 支持绝对路径和相对路径。目录不存在时会自动创建。
        /// 示例:"D:\\Logs\\DDBot\\images" 或 "debug_images"
        /// </summary>
        public string DebugImageDir { get; set; } = "";

        #endregion

        #region Token使用记录配置

        /// <summary>
        /// 是否启用Token使用记录(默认true)
        /// 开启后会记录每次AI调用的token消耗到数据库
        /// </summary>
        public bool EnableTokenUsageRecording { get; set; } = true;

        /// <summary>
        /// 是否记录Token使用明细(默认false)
        /// true:记录每次调用的详细信息(数据量较大)
        /// false:仅记录聚合统计数据(按天/小时+模型+接口类型)
        /// </summary>
        public bool RecordTokenUsageDetail { get; set; } = false;

        #endregion
    }
}
