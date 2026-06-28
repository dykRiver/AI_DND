using DHY.Game.AI.Dtos;
using Microsoft.Extensions.Logging;

namespace DHY.Game.AI.Services;

/// <summary>
/// 叙事后处理验证服务（纯规则，不调用AI）
/// </summary>
public class NarrativeValidatorService : ITransient
{
    private readonly ILogger<NarrativeValidatorService> _logger;

    public NarrativeValidatorService(ILogger<NarrativeValidatorService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 验证叙事文本
    /// </summary>
    public Task<ValidationResult> ValidateAsync(string narrative, ValidationContext context)
    {
        var result = new ValidationResult
        {
            IsValid = true,
            SanitizedText = narrative
        };

        if (string.IsNullOrWhiteSpace(narrative))
        {
            result.IsValid = false;
            result.ShouldRegenerate = true;
            result.Issues.Add(new ValidationIssue
            {
                IssueType = "empty",
                Description = "叙事文本为空",
                Severity = "block"
            });
            return Task.FromResult(result);
        }

        // 规则1: HP为0却正常行动
        if (context.Character != null && context.Character.CurrentHp <= 0)
        {
            // 检查文本中是否描写了正常行动（简单启发式检测）
            var activeKeywords = new[] { "你走向", "你跑向", "你攻击", "你举起", "你冲向" };
            foreach (var keyword in activeKeywords)
            {
                if (narrative.Contains(keyword))
                {
                    result.IsValid = false;
                    result.ShouldRegenerate = true;
                    result.Issues.Add(new ValidationIssue
                    {
                        IssueType = "hp_violation",
                        Description = $"角色HP为0但叙事包含正常行动: {keyword}",
                        Severity = "block"
                    });
                    break;
                }
            }
        }

        // 规则2: 信息泄露
        foreach (var hiddenKeyword in context.HiddenContent)
        {
            if (!string.IsNullOrEmpty(hiddenKeyword) && narrative.Contains(hiddenKeyword))
            {
                result.IsValid = false;
                result.ShouldRegenerate = true;
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "info_leak",
                    Description = $"叙事文本包含隐藏信息关键词: {hiddenKeyword}",
                    Severity = "block"
                });
            }
        }

        // 规则3: 字数越界
        var wordCount = narrative.Length;
        if (wordCount > context.MaxWordCount)
        {
            result.Issues.Add(new ValidationIssue
            {
                IssueType = "word_overflow",
                Description = $"字数越界: {wordCount}/{context.MaxWordCount}",
                Severity = "warn"
            });

            // 尝试截断到合理位置
            var truncated = TruncateNarrative(narrative, context.MaxWordCount);
            result.SanitizedText = truncated;
        }

        // 规则4: NPC矛盾（已死NPC出现对话）
        foreach (var deadNpc in context.DeadNpcs)
        {
            if (string.IsNullOrEmpty(deadNpc)) continue;
        
            // 检查是否有死亡NPC的名字后跟引号（对话）
            if (narrative.Contains(deadNpc) && HasNpcDialoguePattern(narrative, deadNpc))
            {
                result.IsValid = false;
                result.ShouldRegenerate = true;
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "dead_npc_dialogue",
                    Description = $"已死NPC出现对话: {deadNpc}",
                    Severity = "block"
                });
            }
        }

        // 规则5: 禁止表达
        foreach (var forbidden in context.ForbiddenExpressions)
        {
            if (!string.IsNullOrEmpty(forbidden) && narrative.Contains(forbidden))
            {
                result.Issues.Add(new ValidationIssue
                {
                    IssueType = "forbidden_expression",
                    Description = $"包含禁止表达: {forbidden}",
                    Severity = "warn"
                });

                // 标记但不阻断，替换表达
                result.SanitizedText = result.SanitizedText.Replace(forbidden, "");
            }
        }

        // 如果有block级别问题则标记不通过
        if (result.Issues.Any(i => i.Severity == "block"))
        {
            result.IsValid = false;
            result.ShouldRegenerate = true;
        }

        return Task.FromResult(result);
    }

    /// <summary>
    /// 检测文本中是否包含NPC对话模式
    /// </summary>
    private static bool HasNpcDialoguePattern(string text, string npcName)
    {
        // 检查NPC名字后跟对话标志
        var dialogueMarkers = new[] { "\"", "\u300c", "\u201c", "\u9053", "\u8bf4" };
        foreach (var marker in dialogueMarkers)
        {
            if (text.Contains(npcName + marker))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 截断叙事到合理位置（句号、问号、感叹号）
    /// </summary>
    private static string TruncateNarrative(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;

        var truncated = text[..maxLength];

        // 寻找最后一个句号/问号/感叹号
        var lastSentenceEnd = -1;
        var sentenceEnds = new[] { '。', '？', '！', '.', '?', '!' };
        for (var i = truncated.Length - 1; i >= truncated.Length / 2; i--)
        {
            if (sentenceEnds.Contains(truncated[i]))
            {
                lastSentenceEnd = i;
                break;
            }
        }

        return lastSentenceEnd > 0 ? truncated[..(lastSentenceEnd + 1)] : truncated;
    }
}
