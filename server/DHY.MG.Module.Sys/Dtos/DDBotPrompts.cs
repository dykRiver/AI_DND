using System.Text;

namespace DHY.MG.Module.Sys.Dtos
{
    /// <summary>
    /// DDBot Prompt 模板管理
    /// </summary>
    public static class DDBotPrompts
    {
        /// <summary>
        /// OCR 视觉模型识别会话列表的提示词
        /// </summary>
        public const string OCR_PROMPT = @"你是钉钉会话列表截图的结构化识别专家。

【视觉布局结构】
截图显示的是一个垂直滚动的会话列表，每行是一个会话项。每个会话项的视觉结构如下：

┌─────────────────────────────────────┐
│ 会话名称 [标签]              时间   │
└─────────────────────────────────────┘

- 左边是会话名称（可能包含编号如""110""、""85""等）
- 名称右侧可能有蓝色标签如[内部群]、[部门]、[全员]，这些标签不属于名称
- 最右边是时间（如 10:27、09:30、昨天、02-06）
- 关键视觉分隔符：红色横线用于明确分隔不同的会话项

【识别规则】
1. **严格按从上到下的顺序识别每一个会话项，绝对不能跳过任何一行**
2. 判断标准：如果一行文字的最右侧有时间文本（格式如 HH:MM、MM-DD、或""昨天""），则该行是一个会话项
3. 名称 = 行首到标签/时间之前的文字（不含蓝色标签，保留编号前缀）
4. 时间 = 行最右侧的时间文字
5. 标签（[内部群]、[部门]、[全员]等）不要包含在名称中
6. 红线分隔原则：红色横线是强分隔信号，表示一个会话的结束和下一个会话的开始

【特别强调 - 绝对不能忽略】
- **红线边界识别**：将红色横线视为会话分界线，确保红线两侧的会话被独立识别
- **禁止跳过原则**：任何包含时间信息的行都是有效会话，无论名称是什么
- **顺序保证**：严格按照视觉上的从上到下顺序输出，第一个可见的会话必须是输出的第一个
- **完整性要求**：必须识别所有7个可见会话项，不能遗漏或合并

【输出格式】
{
  ""sessions"": [
    {
      ""name"": ""会话名称（不含蓝色标签，保留编号前缀）"",
      ""time"": ""最右侧的时间""
    }
  ]
}

要求：
- 只输出 name、time 两个字段
- 不要输出坐标信息
- 必须提取**完整**的文字，不要截断
- 会话名称中的编号前缀（如""110""、""85""）必须保留
- 按严格的从上到下视觉顺序输出所有会话项，一个都不能少
- 特别注意红线分隔的会话边界，确保每个会话独立识别";

        /// <summary>
        /// 构建消息分析的系统提示词
        /// </summary>
        public static string BuildAnalysisSystemPrompt(DDBotUserProfile profile)
        {
            var sb = new StringBuilder();
            sb.AppendLine("你是一个企业IM消息分析助手。你的任务是判断一批钉钉消息对指定用户的重要程度。");
            sb.AppendLine();
            sb.AppendLine("用户信息：");
            sb.AppendLine($"- 姓名：{profile.Name}");

            if (!string.IsNullOrWhiteSpace(profile.Role))
                sb.AppendLine($"- 职位：{profile.Role}");

            if (!string.IsNullOrWhiteSpace(profile.FocusDescription))
                sb.AppendLine($"- 关注事项：{profile.FocusDescription}");

            if (profile.Projects != null && profile.Projects.Count > 0)
            {
                sb.AppendLine("- 负责项目：");
                foreach (var p in profile.Projects)
                {
                    var line = $"  - {p.Name}";
                    if (!string.IsNullOrWhiteSpace(p.Focus))
                        line += $"：{p.Focus}";
                    if (p.Keywords != null && p.Keywords.Count > 0)
                        line += $"（关键词：{string.Join("、", p.Keywords)}）";
                    sb.AppendLine(line);
                }
            }

            sb.AppendLine();
            sb.AppendLine(@"评判标准（重要性从高到低）：
1. urgent（紧急）：直接@用户、包含deadline、需要用户立即响应或处理的
2. important（重要）：涉及用户负责项目的技术讨论/方案决策/问题反馈，或需要用户知晓的重要事项
3. normal（一般）：与用户工作可能相关但非紧急的日常信息
4. ignore（忽略）：闲聊、表情、重复确认、与用户完全无关的讨论

注意事项：
- 结合上下文理解消息含义，不要只看单条消息
- 需要用户执行操作或做决定的消息应标记为 urgent 或 important
- 返回严格的 JSON 格式");

            return sb.ToString();
        }

        /// <summary>
        /// 构建消息分析的用户提示词
        /// </summary>
        public static string BuildAnalysisUserPrompt(
            List<DDBotMessageItem> messages,
            string conversationName,
            string userName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"以下是来自钉钉会话「{conversationName}」的最新消息：");
            sb.AppendLine();

            foreach (var m in messages)
            {
                var timePart = !string.IsNullOrWhiteSpace(m.MsgTime) ? $" ({m.MsgTime})" : "";
                sb.AppendLine($"[{m.Id}] {m.Sender ?? "未知"}{timePart}: {m.Content}");
            }

            sb.AppendLine();
            sb.AppendLine($@"请分析每条消息对我（{userName}）的重要程度，返回如下 JSON：
{{""results"": [{{""id"": 1, ""level"": ""urgent|important|normal|ignore"", ""reason"": ""简要原因""}}]}}

只返回 JSON，不要其他内容。");

            return sb.ToString();
        }
    }
}