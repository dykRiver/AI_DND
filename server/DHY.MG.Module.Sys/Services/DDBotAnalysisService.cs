using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using DHY.MG.Module.Sys.Dtos;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Drawing;
using Furion.FriendlyException;
using Path = System.IO.Path;

namespace DHY.MG.Module.Sys.Services
{
    /// <summary>
    /// DDBot 钉钉消息分析服务
    /// 提供会话列表截图识别（API1）和消息重要性分析（API2）两个核心接口
    /// </summary>
    [ApiDescriptionSettings("DDBot")]
    public class DDBotAnalysisService(
        IOptions<AliYunOptions> aliYunOption,
        IOptions<DDBotOptions> ddbotOption,
        DDBotTokenRecordingService tokenRecordingService
    ) : IDynamicApiController, ITransient
    {
        private readonly AliYunOptions _aliYunOption = aliYunOption.Value;
        private readonly DDBotOptions _ddbotOption = ddbotOption.Value;
        private readonly DDBotTokenRecordingService _tokenRecordingService = tokenRecordingService;

        // 无实质内容的消息正则
        private static readonly string[] TrivialPatterns = new[]
        {
            @"^(收到|好的|OK|ok|Ok|嗯|嗯嗯|是的|对的|了解|明白|知道了|谢谢|Thanks|感谢|辛苦了|666|👍|👌|🙏|✅|✔)$",
            @"^[👍👌🙏✅✔💪🎉😂😄🤝]+$",
            @"^\[.+\]$"
        };

        #region API1: 会话列表截图识别

        /// <summary>
        /// 识别钉钉会话列表截图
        /// 接收原始截图 → 裁剪/涂白 → AI OCR识别 → 返回会话列表
        /// </summary>
        [DisplayName("识别钉钉会话列表截图")]
        [HttpPost("recognize")]
        public async Task<ChatListRecognizeOutput> RecognizeChatList(ChatListRecognizeInput input)
        {
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(input.ImageBase64))
                throw Oops.Oh("图片数据不能为空");

            try
            {
                // 1. Base64 → Image
                var imageBytes = Convert.FromBase64String(input.ImageBase64);
                using var image = Image.Load<Rgba32>(imageBytes);

                if (_ddbotOption.EnableDebugLog)
                    Debug.WriteLine($"[DDBot] 原始图像尺寸: {image.Width}x{image.Height}");

                // 2. 图像预处理（裁剪 + 涂白 + 缩放）
                using var processedImage = PreprocessImage(image);

                if (_ddbotOption.EnableDebugLog)
                    Debug.WriteLine($"[DDBot] 处理后图像尺寸: {processedImage.Width}x{processedImage.Height}");

                // 2.1 保存调试图片（如已启用）
                if (_ddbotOption.SaveDebugImage)
                    SaveDebugImageToFile(processedImage);

                // 3. 转Base64
                var processedBase64 = ImageToBase64(processedImage);

                // 4. 调用OCR视觉模型
                var ocrCallSw = Stopwatch.StartNew();
                var ocrResult = await CallVisionOcrAI(processedBase64);
                ocrCallSw.Stop();

                if (string.IsNullOrWhiteSpace(ocrResult))
                    throw Oops.Oh("OCR识别返回空结果");

                // 4.1 记录Token使用(OCR识别)
                if (_ddbotOption.EnableTokenUsageRecording)
                {
                    try
                    {
                        await _tokenRecordingService.RecordTokenFailure(
                            _ddbotOption.FirstStageOcrModel, // 简化处理,实际应该记录真实模型
                            "recognize",
                            ocrCallSw.ElapsedMilliseconds,
                            null);
                    }
                    catch { /* 忽略记录失败 */ }
                }

                // 5. 解析JSON
                var sessions = ParseOcrResponse(ocrResult);

                // 6. 后处理
                sessions = PostProcessSessions(sessions);

                // 7. 计算固定布局坐标
                var result = new ChatListRecognizeOutput
                {
                    Sessions = new List<ChatListSessionItem>(),
                    ProcessTimeMs = sw.ElapsedMilliseconds
                };

                for (int i = 0; i < sessions.Count; i++)
                {
                    var (x, y) = CalculateFixedCoordinate(i + 1);
                    result.Sessions.Add(new ChatListSessionItem
                    {
                        Index = i + 1,
                        Name = sessions[i].Name,
                        Time = sessions[i].Time,
                        X = x,
                        Y = y
                    });
                }

                result.TotalCount = result.Sessions.Count;
                result.ProcessTimeMs = sw.ElapsedMilliseconds;

                return result;
            }
            catch (FormatException)
            {
                throw Oops.Oh("图片Base64格式无效");
            }
            catch (Exception ex) when (ex is not AppFriendlyException)
            {
                if (_ddbotOption.EnableDebugLog)
                    Debug.WriteLine($"[DDBot] RecognizeChatList异常: {ex.Message}");
                
                // 记录失败的token使用
                if (_ddbotOption.EnableTokenUsageRecording)
                {
                    try
                    {
                        await _tokenRecordingService.RecordTokenFailure(
                            _ddbotOption.FirstStageOcrModel,
                            "recognize",
                            sw.ElapsedMilliseconds,
                            ex.Message);
                    }
                    catch { /* 忽略记录失败 */ }
                }
                
                throw Oops.Oh($"会话列表识别失败: {ex.Message}");
            }
        }

        #endregion

        #region API2: 消息重要性分析

        /// <summary>
        /// 分析钉钉消息重要性
        /// 接收消息列表和用户配置 → 规则预筛 + AI分析 → 返回重要性结果
        /// </summary>
        [DisplayName("分析钉钉消息重要性")]
        [HttpPost("analyze")]
        public async Task<MessageAnalyzeOutput> AnalyzeMessages(MessageAnalyzeInput input)
        {
            var sw = Stopwatch.StartNew();

            if (input.Messages == null || input.Messages.Count == 0)
                throw Oops.Oh("消息列表不能为空");

            if (input.UserProfile == null || string.IsNullOrWhiteSpace(input.UserProfile.Name))
                throw Oops.Oh("用户信息不能为空");

            var allResults = new List<MessageAnalysisResultItem>();
            var needAi = new List<DDBotMessageItem>();

            // 阶段1: 规则预筛
            if (_ddbotOption.EnableRulePreFilter)
            {
                foreach (var msg in input.Messages)
                {
                    var ruleResult = RulePreFilter(msg, input.UserProfile, input.ConversationType);
                    if (ruleResult != null)
                    {
                        allResults.Add(ruleResult);
                    }
                    else
                    {
                        needAi.Add(msg);
                    }
                }
            }
            else
            {
                needAi.AddRange(input.Messages);
            }

            // 阶段2: AI批量分析
            if (needAi.Count > 0)
            {
                var batchSize = _ddbotOption.BatchSize;
                for (int i = 0; i < needAi.Count; i += batchSize)
                {
                    var batch = needAi.Skip(i).Take(batchSize).ToList();
                    try
                    {
                        var aiResults = await AnalyzeBatchWithAI(batch, input.UserProfile, input.ConversationName);
                        allResults.AddRange(aiResults);
                    }
                    catch (Exception ex)
                    {
                        if (_ddbotOption.EnableDebugLog)
                            Debug.WriteLine($"[DDBot] AI分析批次失败: {ex.Message}");

                        // AI失败时，将该批次消息标记为normal
                        foreach (var msg in batch)
                        {
                            allResults.Add(new MessageAnalysisResultItem
                            {
                                Id = msg.Id,
                                Fingerprint = msg.Fingerprint,
                                Level = "normal",
                                Reason = "AI分析失败，默认标记",
                                Method = "ai"
                            });
                        }
                    }
                }
            }

            sw.Stop();

            return new MessageAnalyzeOutput
            {
                Results = allResults,
                TotalCount = input.Messages.Count,
                UrgentCount = allResults.Count(r => r.Level == "urgent"),
                ImportantCount = allResults.Count(r => r.Level == "important"),
                ProcessTimeMs = sw.ElapsedMilliseconds
            };
        }

        #endregion

        #region 图像处理

        /// <summary>
        /// 图像预处理：裁剪 → 涂白 → 缩放
        /// </summary>
        private Image<Rgba32> PreprocessImage(Image<Rgba32> image)
        {
            // 1. 裁剪
            var cropped = CropImage(image);

            // 2. 涂白预览区域
            if (_ddbotOption.MaskPreview)
            {
                MaskPreviewAreas(cropped);
            }

            // 3. 绘制固定位置红线（调试用）
            if (_ddbotOption.DrawFixedRedLine)
            {
                DrawFixedRedLine(cropped);
            }

            // 4. 缩放（如果超过最大宽度）
            if (cropped.Width > _ddbotOption.MaxImageWidth)
            {
                var ratio = (double)_ddbotOption.MaxImageWidth / cropped.Width;
                var newHeight = (int)(cropped.Height * ratio);
                cropped.Mutate(x => x.Resize(_ddbotOption.MaxImageWidth, newHeight));
            }

            return cropped;
        }

        /// <summary>
        /// 裁剪图像：去除头像区域(左)、导航栏(上)等干扰区域
        /// </summary>
        private Image<Rgba32> CropImage(Image<Rgba32> image)
        {
            int left = _ddbotOption.CropLeft;
            int top = _ddbotOption.CropTop;
            int right = _ddbotOption.CropRight;

            int cropWidth = image.Width - left - right;
            int cropHeight;

            if (_ddbotOption.CropTargetHeight > 0)
            {
                // 动态计算底部裁剪使最终高度等于目标值
                cropHeight = Math.Min(_ddbotOption.CropTargetHeight, image.Height - top);
            }
            else
            {
                cropHeight = image.Height - top - _ddbotOption.CropBottom;
            }

            // 确保裁剪区域有效
            cropWidth = Math.Max(1, Math.Min(cropWidth, image.Width - left));
            cropHeight = Math.Max(1, Math.Min(cropHeight, image.Height - top));

            return image.Clone(x => x.Crop(new Rectangle(left, top, cropWidth, cropHeight)));
        }

        /// <summary>
        /// 涂白预览消息区域，减少AI识别干扰
        /// 基于固定布局计算每个会话的预览消息区域位置
        /// </summary>
        private void MaskPreviewAreas(Image<Rgba32> image)
        {
            // 在裁剪后的图像上操作，坐标需减去裁剪偏移
            int firstYInCropped = _ddbotOption.LayoutFirstY - _ddbotOption.CropTop;
            int spacing = _ddbotOption.LayoutSpacing;
            int maskHeight = _ddbotOption.MaskHeight;
            int offsetY = _ddbotOption.MaskOffsetY;

            // 计算涂白的X范围（与 Python 端 mask_preview_areas 逻辑保持一致）
            // MaskMarginLeft=0 时 fallback 到 CropLeft，再减去 CropLeft 偏移量得到裁剪后坐标
            int marginLeft = _ddbotOption.MaskMarginLeft;
            if (marginLeft <= 0)
                marginLeft = _ddbotOption.CropLeft;   // fallback：与 Python 端一致
            int maskLeft = marginLeft - _ddbotOption.CropLeft; // 减去裁剪偏移，转换到裁剪后图像坐标
            if (maskLeft < 0) maskLeft = 0;
            int maskRight = image.Width - _ddbotOption.MaskMarginRight;
            if (maskRight <= maskLeft)
                maskRight = image.Width;

            int maskWidth = maskRight - maskLeft;

            // 遍历可能的会话位置
            int maxSessions = (image.Height - firstYInCropped) / spacing + 2;
            for (int i = 0; i < maxSessions; i++)
            {
                int centerY = firstYInCropped + i * spacing;
                int maskY = centerY + offsetY;

                if (maskY < 0) continue;
                if (maskY + maskHeight > image.Height) break;

                // 填充白色
                var fillRect = new Rectangle(maskLeft, maskY, maskWidth, maskHeight);
                image.Mutate(x => x.Fill(Color.White, new RectangularPolygon(fillRect)));
                
                // 添加红色下边框（1像素宽）
                var borderBottom = new Rectangle(maskLeft, maskY + maskHeight - 1, maskWidth, 1);
                image.Mutate(x => x.Fill(Color.Red, new RectangularPolygon(borderBottom)));
            }
        }

        /// <summary>
        /// 在固定位置绘制红线（用于调试参考）
        /// </summary>
        private void DrawFixedRedLine(Image<Rgba32> image)
        {
            int lineY = _ddbotOption.FixedRedLineY;
            
            // 确保Y坐标在图像范围内
            if (lineY < 0 || lineY >= image.Height)
                return;

            // 绘制横贯整个图像宽度的红线（1像素高）
            var redLine = new Rectangle(0, lineY, image.Width, 1);
            image.Mutate(x => x.Fill(Color.Red, new RectangularPolygon(redLine)));
        }

        /// <summary>
        /// 计算固定布局坐标（返回相对于原始截图的坐标）
        /// </summary>
        private (int x, int y) CalculateFixedCoordinate(int index)
        {
            int x = _ddbotOption.LayoutFirstX;
            int y = _ddbotOption.LayoutFirstY + (index - 1) * _ddbotOption.LayoutSpacing;
            return (x, y);
        }

        /// <summary>
        /// 将Image转为Base64字符串
        /// </summary>
        private static string ImageToBase64(Image image)
        {
            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return Convert.ToBase64String(ms.ToArray());
        }

        /// <summary>
        /// 保存调试图片到本地目录（文件名包含时间戏）
        /// </summary>
        private void SaveDebugImageToFile(Image image)
        {
            try
            {
                var dir = string.IsNullOrWhiteSpace(_ddbotOption.DebugImageDir)
                    ? Path.Combine(AppContext.BaseDirectory, "debug_images")
                    : Path.IsPathRooted(_ddbotOption.DebugImageDir)
                        ? _ddbotOption.DebugImageDir
                        : Path.Combine(AppContext.BaseDirectory, _ddbotOption.DebugImageDir);

                Directory.CreateDirectory(dir);

                var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                var filePath = Path.Combine(dir, $"processed_{ts}.png");

                image.Save(filePath, new PngEncoder());

                if (_ddbotOption.EnableDebugLog)
                    Debug.WriteLine($"[DDBot] 调试图片已保存: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DDBot] 保存调试图片失败: {ex.Message}");
            }
        }

        #endregion

        #region AI调用

        /// <summary>
        /// 调用视觉OCR模型识别截图（支持双阶段识别）
        /// </summary>
        private async Task<string> CallVisionOcrAI(string base64Image)
        {
            string apiKey = _aliYunOption.DashScopeApiKey;
            string url = _aliYunOption.DashScopeEndpoint;

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("DashScopeApiKey 配置为空");
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("DashScopeEndpoint 配置为空");

            // 第一阶段：使用便宜的OCR模型快速识别
            if (_ddbotOption.EnableTwoStageRecognition)
            {
                var firstStageResult = await CallOcrWithModel(
                    base64Image, 
                    _ddbotOption.FirstStageOcrModel, 
                    apiKey, 
                    url, 
                    _ddbotOption.FirstStageOcrEnableThinking,
                    _ddbotOption.FirstStageOcrMaxTokens);
                
                // 解析第一阶段结果，检查会话数量
                var firstStageSessions = ParseOcrResponse(firstStageResult);
                if (firstStageSessions.Count == 7)
                {
                    if (_ddbotOption.EnableDebugLog)
                        Console.WriteLine($"[DDBot] 第一阶段识别成功，获得 {firstStageSessions.Count} 个会话，无需第二阶段");
                    return firstStageResult;
                }
                else
                {
                    if (_ddbotOption.EnableDebugLog)
                        Console.WriteLine($"[DDBot] 第一阶段识别到 {firstStageSessions.Count} 个会话（期望7个），启动第二阶段识别");
                }
            }

            // 第二阶段：使用强大的模型重新识别
            string finalModel = _ddbotOption.SecondStageOcrModel;
            bool enableThinking = _ddbotOption.SecondStageOcrEnableThinking;
            int maxTokens = _ddbotOption.SecondStageOcrMaxTokens;
            
            return await CallOcrWithModel(base64Image, finalModel, apiKey, url, enableThinking, maxTokens);
        }

        /// <summary>
        /// 使用指定模型调用OCR识别
        /// </summary>
        private async Task<string> CallOcrWithModel(string base64Image, string modelName, string apiKey, string url, bool enableThinking, int maxTokens = 0)
        {
            // 如果没有指定maxTokens，使用第二阶段的默认值
            if (maxTokens <= 0)
                maxTokens = _ddbotOption.SecondStageOcrMaxTokens;

            for (int attempt = 0; attempt <= _ddbotOption.MaxRetries; attempt++)
            {
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    // 增加超时时间，特别是对于启用思考模式的qwen3.5-plus模型
                    int timeoutSeconds = enableThinking ? _aliYunOption.DefaultTimeout * 2 : _aliYunOption.DefaultTimeout;
                    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
                    
                    // 添加一些额外的HTTP配置来改善连接稳定性
                    client.DefaultRequestHeaders.ConnectionClose = false;

                    // 视觉模型使用multimodal content格式
                    object requestBody;
                    if (enableThinking)
                    {
                        // 启用思考模式（qwen3系列支持）
                        requestBody = new
                        {
                            model = modelName,
                            messages = new[]
                            {
                                new
                                {
                                    role = "user",
                                    content = new object[]
                                    {
                                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } },
                                        new { type = "text", text = DDBotPrompts.OCR_PROMPT }
                                    }
                                }
                            },
                            max_tokens = maxTokens,
                            stream = false,
                            enable_thinking = true
                        };
                    }
                    else
                    {
                        // 不启用思考模式
                        requestBody = new
                        {
                            model = modelName,
                            messages = new[]
                            {
                                new
                                {
                                    role = "user",
                                    content = new object[]
                                    {
                                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{base64Image}" } },
                                        new { type = "text", text = DDBotPrompts.OCR_PROMPT }
                                    }
                                }
                            },
                            max_tokens = maxTokens,
                            stream = false
                        };
                    }

                    // 添加调试日志
                    if (_ddbotOption.EnableDebugLog)
                    {
                        Console.WriteLine($"[DDBot] 使用模型: {modelName}, 思考模式: {enableThinking}");
                        Console.WriteLine($"[DDBot] 使用的提示词长度: {DDBotPrompts.OCR_PROMPT.Length} 字符");
                        Console.WriteLine($"[DDBot] 完整提示词:");
                        Console.WriteLine(DDBotPrompts.OCR_PROMPT);
                        Console.WriteLine("[DDBot] 提示词结束");
                    }

                    // 记录完整OCR请求参数
                    if (_ddbotOption.LogFullAiRequests)
                    {
                        Debug.WriteLine($"[DDBot] OCR AI完整请求参数:");
                        Debug.WriteLine($"  Model: {modelName}");
                        Debug.WriteLine($"  EnableThinking: {enableThinking}");
                        Debug.WriteLine($"  MaxTokens: {maxTokens}");
                        Debug.WriteLine($"  Base64图像长度: {base64Image.Length} 字符");
                        Debug.WriteLine($"  提示词长度: {DDBotPrompts.OCR_PROMPT.Length} 字符");
                        Debug.WriteLine($"  提示词预览: {DDBotPrompts.OCR_PROMPT.Substring(0, Math.Min(200, DDBotPrompts.OCR_PROMPT.Length))}...");
                    }

                    var jsonContent = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(new Uri(url), content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (_ddbotOption.EnableDebugLog)
                            Debug.WriteLine($"[DDBot] OCR AI调用失败 - 状态码: {response.StatusCode}, 响应: {responseBody}");
                        throw new Exception($"OCR API调用失败，状态码: {response.StatusCode}");
                    }

                    var result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    string resultContent = result.choices[0].message.content.ToString();

                    // 记录完整OCR响应
                    if (_ddbotOption.LogFullAiResponses)
                    {
                        Debug.WriteLine($"[DDBot] OCR AI完整响应:");
                        Debug.WriteLine($"  使用模型: {modelName}");
                        Debug.WriteLine($"  响应长度: {resultContent.Length} 字符");
                        Debug.WriteLine($"  完整响应内容:");
                        Debug.WriteLine(resultContent);
                        
                        // 保存到独立文件
                        if (_ddbotOption.SaveFullAiResponseToFile)
                        {
                            await SaveAiResponseToFile(resultContent, "ocr");
                        }
                    }
                    else if (_ddbotOption.EnableDebugLog)
                    {
                        Debug.WriteLine($"[DDBot] OCR AI返回 (模型:{modelName}): {resultContent[..Math.Min(500, resultContent.Length)]}");
                    }

                    return resultContent;
                }
                catch (Exception ex)
                {
                    if (_ddbotOption.EnableDebugLog)
                        Debug.WriteLine($"[DDBot] OCR AI调用失败 (第{attempt + 1}次, 模型:{modelName}): {ex.Message}");

                    if (attempt < _ddbotOption.MaxRetries)
                        await Task.Delay((int)Math.Pow(2, attempt) * 1000);
                }
            }

            throw new Exception($"OCR AI调用全部失败 (模型:{modelName})");
        }

        /// <summary>
        /// 调用文本分析AI（非流式）
        /// 参照 HGTGameService.CallAliYunAI 的实现模式
        /// </summary>
        private async Task<string> CallAnalysisAI(string systemPrompt, string userMessage)
        {
            string apiKey = _aliYunOption.DashScopeApiKey;
            string url = _aliYunOption.DashScopeEndpoint;

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new Exception("DashScopeApiKey 配置为空");
            if (string.IsNullOrWhiteSpace(url))
                throw new Exception("DashScopeEndpoint 配置为空");

            // 记录完整请求参数
            if (_ddbotOption.LogFullAiRequests)
            {
                Debug.WriteLine($"[DDBot] AI分析完整请求参数:");
                Debug.WriteLine($"  Model: {_ddbotOption.AnalysisModel}");
                Debug.WriteLine($"  Temperature: {_ddbotOption.AnalysisTemperature}");
                Debug.WriteLine($"  MaxTokens: {_ddbotOption.AnalysisMaxTokens}");
                Debug.WriteLine($"  SystemPrompt长度: {systemPrompt.Length} 字符");
                Debug.WriteLine($"  UserMessage长度: {userMessage.Length} 字符");
                Debug.WriteLine($"  SystemPrompt预览: {systemPrompt.Substring(0, Math.Min(200, systemPrompt.Length))}...");
                Debug.WriteLine($"  UserMessage预览: {userMessage.Substring(0, Math.Min(200, userMessage.Length))}...");
            }

            bool enableThinking = _ddbotOption.AnalysisEnableThinking;
            int thinkingBudget = _ddbotOption.AnalysisThinkingBudget;

            for (int attempt = 0; attempt <= _ddbotOption.MaxRetries; attempt++)
            {
                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
                    // 思考模式响应时间显著增加，需要双倍超时并保持连接复用
                    client.Timeout = TimeSpan.FromSeconds(_aliYunOption.DefaultTimeout * (enableThinking ? 2 : 1));
                    client.DefaultRequestHeaders.ConnectionClose = false;

                    string jsonContent;
                    if (enableThinking)
                    {
                        // 思考模式必须使用流式输出
                        var requestBodyThinking = new
                        {
                            model = _ddbotOption.AnalysisModel,
                            messages = new[]
                            {
                                new { role = "system", content = systemPrompt },
                                new { role = "user", content = userMessage }
                            },
                            temperature = _ddbotOption.AnalysisTemperature,
                            max_tokens = _ddbotOption.AnalysisMaxTokens,
                            enable_thinking = true,
                            thinking_budget = thinkingBudget,
                            stream = true
                        };
                        jsonContent = JsonConvert.SerializeObject(requestBodyThinking);
                    }
                    else
                    {
                        var requestBody = new
                        {
                            model = _ddbotOption.AnalysisModel,
                            messages = new[]
                            {
                                new { role = "system", content = systemPrompt },
                                new { role = "user", content = userMessage }
                            },
                            temperature = _ddbotOption.AnalysisTemperature,
                            max_tokens = _ddbotOption.AnalysisMaxTokens,
                            result_format = "message"
                        };
                        jsonContent = JsonConvert.SerializeObject(requestBody);
                    }

                    // 记录完整请求参数（日志部分放在构造请求体之后）
                    if (_ddbotOption.LogFullAiRequests)
                    {
                        Debug.WriteLine($"[DDBot] AI分析请求参数:");
                        Debug.WriteLine($"  EnableThinking: {enableThinking}");
                        if (enableThinking) Debug.WriteLine($"  ThinkingBudget: {thinkingBudget}");
                    }

                    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                    string resultContent;

                    if (enableThinking)
                    {
                        // 流式聚合：读取所有 SSE 帧，拼接 content 字段
                        var requestMsg = new HttpRequestMessage(HttpMethod.Post, new Uri(url));
                        requestMsg.Headers.Add("Accept", "text/event-stream");
                        requestMsg.Content = httpContent;

                        using var response = await client.SendAsync(requestMsg, HttpCompletionOption.ResponseHeadersRead);

                        if (!response.IsSuccessStatusCode)
                        {
                            var errBody = await response.Content.ReadAsStringAsync();
                            if (_ddbotOption.EnableDebugLog)
                                Debug.WriteLine($"[DDBot] 分析AI调用失败(thinking) - 状态码: {response.StatusCode}, 响应: {errBody}");
                            throw new Exception($"分析API调用失败，状态码: {response.StatusCode}");
                        }

                        using var stream = await response.Content.ReadAsStreamAsync();
                        using var reader = new StreamReader(stream);
                        var sb = new System.Text.StringBuilder();
                        while (!reader.EndOfStream)
                        {
                            var line = await reader.ReadLineAsync();
                            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data:")) continue;
                            var dataJson = line.Substring(5).Trim();
                            if (dataJson == "[DONE]") break;
                            try
                            {
                                var chunk = JsonConvert.DeserializeObject<dynamic>(dataJson);
                                var deltaContent = chunk?.choices?[0]?.delta?.content?.ToString();
                                if (!string.IsNullOrEmpty(deltaContent))
                                    sb.Append(deltaContent);
                            }
                            catch { /* 忽略单帧解析异常 */ }
                        }
                        resultContent = sb.ToString();
                    }
                    else
                    {
                        var response = await client.PostAsync(new Uri(url), httpContent);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            if (_ddbotOption.EnableDebugLog)
                                Debug.WriteLine($"[DDBot] 分析AI调用失败 - 状态码: {response.StatusCode}, 响应: {responseBody}");
                            throw new Exception($"分析API调用失败，状态码: {response.StatusCode}");
                        }

                        var result = JsonConvert.DeserializeObject<dynamic>(responseBody);
                        resultContent = result.choices[0].message.content.ToString();
                    }

                    // 记录完整响应
                    if (_ddbotOption.LogFullAiResponses)
                    {
                        Debug.WriteLine($"[DDBot] AI分析完整响应:");
                        Debug.WriteLine($"  响应长度: {resultContent.Length} 字符");
                        Debug.WriteLine($"  完整响应内容:");
                        Debug.WriteLine(resultContent);

                        // 保存到独立文件
                        if (_ddbotOption.SaveFullAiResponseToFile)
                        {
                            await SaveAiResponseToFile(resultContent, "analysis");
                        }
                    }
                    else if (_ddbotOption.EnableDebugLog)
                    {
                        Debug.WriteLine($"[DDBot] 分析AI返回: {resultContent[..Math.Min(500, resultContent.Length)]}");
                    }

                    return resultContent;
                }
                catch (Exception ex)
                {
                    if (_ddbotOption.EnableDebugLog)
                        Debug.WriteLine($"[DDBot] 分析AI调用失败 (第{attempt + 1}次): {ex.Message}");

                    if (attempt < _ddbotOption.MaxRetries)
                        await Task.Delay((int)Math.Pow(2, attempt) * 1000);
                }
            }

            throw new Exception("分析AI调用全部失败");
        }

        #endregion

        #region 规则预筛

        /// <summary>
        /// 规则预筛：本地零成本判断消息重要性
        /// 返回null表示需要送AI分析
        /// </summary>
        private MessageAnalysisResultItem? RulePreFilter(
            DDBotMessageItem msg,
            DDBotUserProfile profile,
            string conversationType)
        {
            var content = msg.Content ?? "";

            // 1. @我 → urgent
            if (profile.AtMeAlwaysUrgent && IsAtMe(content, profile))
            {
                return new MessageAnalysisResultItem
                {
                    Id = msg.Id,
                    Fingerprint = msg.Fingerprint,
                    Level = "urgent",
                    Reason = "消息中@了你",
                    Method = "rule"
                };
            }

            // 2. @所有人 → important
            if (profile.AtAllAlwaysImportant &&
                (content.Contains("@所有人") || content.Contains("@all", StringComparison.OrdinalIgnoreCase)))
            {
                return new MessageAnalysisResultItem
                {
                    Id = msg.Id,
                    Fingerprint = msg.Fingerprint,
                    Level = "important",
                    Reason = "消息@了所有人",
                    Method = "rule"
                };
            }

            // 3. 紧急关键词 → urgent
            if (profile.Keywords?.Urgent != null)
            {
                foreach (var kw in profile.Keywords.Urgent)
                {
                    if (!string.IsNullOrWhiteSpace(kw) && content.Contains(kw))
                    {
                        return new MessageAnalysisResultItem
                        {
                            Id = msg.Id,
                            Fingerprint = msg.Fingerprint,
                            Level = "urgent",
                            Reason = $"包含紧急关键词: {kw}",
                            Method = "rule"
                        };
                    }
                }
            }

            // 4. 项目关键词 → important
            if (profile.Projects != null)
            {
                foreach (var proj in profile.Projects)
                {
                    if (proj.Keywords == null) continue;
                    foreach (var kw in proj.Keywords)
                    {
                        if (!string.IsNullOrWhiteSpace(kw) && content.Contains(kw))
                        {
                            return new MessageAnalysisResultItem
                            {
                                Id = msg.Id,
                                Fingerprint = msg.Fingerprint,
                                Level = "important",
                                Reason = $"涉及关注项目: {proj.Name} ({kw})",
                                Method = "rule"
                            };
                        }
                    }
                }
            }

            // 5. 重要关键词 → important
            if (profile.Keywords?.Important != null)
            {
                foreach (var kw in profile.Keywords.Important)
                {
                    if (!string.IsNullOrWhiteSpace(kw) && content.Contains(kw))
                    {
                        return new MessageAnalysisResultItem
                        {
                            Id = msg.Id,
                            Fingerprint = msg.Fingerprint,
                            Level = "important",
                            Reason = $"包含重要关键词: {kw}",
                            Method = "rule"
                        };
                    }
                }
            }

            // 6. 无实质内容 → ignore
            if (IsTrivial(content))
            {
                return new MessageAnalysisResultItem
                {
                    Id = msg.Id,
                    Fingerprint = msg.Fingerprint,
                    Level = "ignore",
                    Reason = "无实质内容",
                    Method = "rule"
                };
            }

            // 7. 私聊 → important
            if (profile.PrivateAlwaysImportant && conversationType == "private")
            {
                return new MessageAnalysisResultItem
                {
                    Id = msg.Id,
                    Fingerprint = msg.Fingerprint,
                    Level = "important",
                    Reason = "私聊消息",
                    Method = "rule"
                };
            }

            return null;
        }

        /// <summary>
        /// 检测消息是否@了用户
        /// </summary>
        private static bool IsAtMe(string content, DDBotUserProfile profile)
        {
            if (content.Contains($"@{profile.Name}"))
                return true;

            if (profile.Aliases != null)
            {
                foreach (var alias in profile.Aliases)
                {
                    if (!string.IsNullOrWhiteSpace(alias) && content.Contains($"@{alias}"))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 检测消息是否为无实质内容（表情、简短确认等）
        /// </summary>
        private static bool IsTrivial(string content)
        {
            var text = content.Trim();
            if (text.Length <= 1) return true;

            foreach (var pattern in TrivialPatterns)
            {
                if (Regex.IsMatch(text, pattern))
                    return true;
            }

            return false;
        }

        #endregion

        #region AI批量分析

        /// <summary>
        /// 使用AI批量分析一批消息的重要性
        /// </summary>
        private async Task<List<MessageAnalysisResultItem>> AnalyzeBatchWithAI(
            List<DDBotMessageItem> messages,
            DDBotUserProfile profile,
            string conversationName)
        {
            var systemPrompt = DDBotPrompts.BuildAnalysisSystemPrompt(profile);
            var userPrompt = DDBotPrompts.BuildAnalysisUserPrompt(messages, conversationName, profile.Name);

            var responseText = await CallAnalysisAI(systemPrompt, userPrompt);
            return ParseAnalysisResponse(responseText, messages);
        }

        #endregion

        #region 结果解析

        /// <summary>
        /// 解析OCR模型返回的JSON结果
        /// </summary>
        private List<OcrSessionRaw> ParseOcrResponse(string responseText)
        {
            // 从 markdown code block 中提取 JSON
            string jsonStr;
            if (responseText.Contains("```json"))
            {
                jsonStr = responseText.Split("```json")[1].Split("```")[0].Trim();
            }
            else if (responseText.Contains("```"))
            {
                jsonStr = responseText.Split("```")[1].Split("```")[0].Trim();
            }
            else
            {
                jsonStr = responseText.Trim();
            }

            var data = JsonConvert.DeserializeObject<dynamic>(jsonStr);
            var sessions = new List<OcrSessionRaw>();

            if (data?.sessions != null)
            {
                foreach (var s in data.sessions)
                {
                    sessions.Add(new OcrSessionRaw
                    {
                        Name = s.name?.ToString() ?? "",
                        Time = s.time?.ToString() ?? ""
                    });
                }
            }

            return sessions;
        }

        /// <summary>
        /// 后处理OCR识别结果：去除标签、修复重复字符
        /// 参考 Python chatlist_recognizer.py 的 _postprocess_sessions
        /// </summary>
        private static List<OcrSessionRaw> PostProcessSessions(List<OcrSessionRaw> sessions)
        {
            foreach (var s in sessions)
            {
                // 1. 去除名称中的标签文本
                var name = s.Name;
                name = Regex.Replace(name, @"[（\(]\s*(?:内部群|部门|全员)\s*[）\)]", "");
                name = Regex.Replace(name, @"\[\s*(?:内部群|部门|全员)\s*\]", "");
                s.Name = name.Trim();

                // 2. 修复3个及以上连续重复的中文字
                s.Name = Regex.Replace(s.Name, @"([\u4e00-\u9fff])\1{2,}", "$1");
            }

            return sessions;
        }

        /// <summary>
        /// 解析AI消息分析返回的JSON结果
        /// </summary>
        private List<MessageAnalysisResultItem> ParseAnalysisResponse(
            string responseText,
            List<DDBotMessageItem> messages)
        {
            var results = new List<MessageAnalysisResultItem>();

            try
            {
                // 从 markdown code block 中提取 JSON
                string jsonStr;
                if (responseText.Contains("```json"))
                    jsonStr = responseText.Split("```json")[1].Split("```")[0].Trim();
                else if (responseText.Contains("```"))
                    jsonStr = responseText.Split("```")[1].Split("```")[0].Trim();
                else
                    jsonStr = responseText.Trim();

                var data = JsonConvert.DeserializeObject<dynamic>(jsonStr);
                var aiResults = data?.results;

                // id → message 映射
                var idToMsg = messages.ToDictionary(m => m.Id, m => m);
                var returnedIds = new HashSet<int>();

                if (aiResults != null)
                {
                    foreach (var item in aiResults)
                    {
                        int msgId = (int)item.id;
                        string level = item.level?.ToString() ?? "normal";
                        string reason = item.reason?.ToString() ?? "";

                        // 验证 level 合法性
                        if (level != "urgent" && level != "important" && level != "normal" && level != "ignore")
                            level = "normal";

                        if (idToMsg.ContainsKey(msgId))
                        {
                            results.Add(new MessageAnalysisResultItem
                            {
                                Id = msgId,
                                Fingerprint = idToMsg[msgId].Fingerprint,
                                Level = level,
                                Reason = reason,
                                Method = "ai"
                            });
                            returnedIds.Add(msgId);
                        }
                    }
                }

                // 对AI未返回的消息，标记为normal
                foreach (var msg in messages)
                {
                    if (!returnedIds.Contains(msg.Id))
                    {
                        results.Add(new MessageAnalysisResultItem
                        {
                            Id = msg.Id,
                            Fingerprint = msg.Fingerprint,
                            Level = "normal",
                            Reason = "AI未返回结果",
                            Method = "ai"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                if (_ddbotOption.EnableDebugLog)
                    Debug.WriteLine($"[DDBot] 解析AI返回结果失败: {ex.Message}, 原文: {responseText[..Math.Min(500, responseText.Length)]}");

                // 解析失败，全部标记为normal
                foreach (var msg in messages)
                {
                    results.Add(new MessageAnalysisResultItem
                    {
                        Id = msg.Id,
                        Fingerprint = msg.Fingerprint,
                        Level = "normal",
                        Reason = "AI结果解析失败",
                        Method = "ai"
                    });
                }
            }

            return results;
        }

        /// <summary>
        /// 保存AI响应到独立日志文件
        /// </summary>
        private async Task SaveAiResponseToFile(string responseContent, string responseType)
        {
            try
            {
                // 确保目录存在
                var logDir = string.IsNullOrWhiteSpace(_ddbotOption.AiResponseLogDir) 
                    ? "ai_logs" 
                    : _ddbotOption.AiResponseLogDir;
                
                var fullPath = Path.GetFullPath(logDir);
                Directory.CreateDirectory(fullPath);

                // 生成文件名：类型_时间戳.log
                var fileName = $"{responseType}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.log";
                var filePath = Path.Combine(fullPath, fileName);

                // 写入响应内容
                await File.WriteAllTextAsync(filePath, responseContent, Encoding.UTF8);
                
                Debug.WriteLine($"[DDBot] AI响应已保存到文件: {filePath}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DDBot] 保存AI响应文件失败: {ex.Message}");
            }
        }

        #endregion

        #region 健康检查接口

        /// <summary>
        /// 健康检查接口 - 用于客户端验证服务是否在线
        /// 不进行任何业务处理，直接返回成功状态
        /// </summary>
        [DisplayName("健康检查")]
        [AllowAnonymous]
        [HttpGet("health")]
        [HttpPost("health")]
        public IActionResult HealthCheck()
        {
            return new JsonResult(new
            {
                code = 200,
                data = new
                {
                    status = "healthy",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    service = "DDBotAnalysisService"
                },
                message = "服务运行正常"
            });
        }

        #endregion

        /// <summary>
        /// OCR解析的原始会话数据（内部使用）
        /// </summary>
        private class OcrSessionRaw
        {
            public string Name { get; set; } = "";
            public string Time { get; set; } = "";
        }

        /// <summary>
        /// AI调用结果(包含token使用信息)
        /// </summary>
        private class AiCallResult
        {
            public string Content { get; set; } = "";
            public int PromptTokens { get; set; }
            public int CompletionTokens { get; set; }
            public int TotalTokens { get; set; }
            public bool IsSuccess { get; set; } = true;
            public string? ErrorMessage { get; set; }
        }
    }
}
