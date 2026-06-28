using DHY.Game.Core.Entities;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 叙事验证上下文
/// </summary>
public class ValidationContext
{
    /// <summary>当前角色状态</summary>
    public GameCharacter? Character { get; set; }

    /// <summary>存活NPC列表</summary>
    public List<string> AliveNpcs { get; set; } = new();

    /// <summary>已死NPC列表</summary>
    public List<string> DeadNpcs { get; set; } = new();

    /// <summary>玩家未获取的隐藏信息关键词</summary>
    public List<string> HiddenContent { get; set; } = new();

    /// <summary>当前场景类型字数上限</summary>
    public int MaxWordCount { get; set; } = 400;

    /// <summary>禁止表达列表</summary>
    public List<string> ForbiddenExpressions { get; set; } = new()
    {
        "你想到", "你认为", "你觉得", "你心想", "你意识到", "你知道"
    };
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    /// <summary>是否通过验证</summary>
    public bool IsValid { get; set; }

    /// <summary>问题列表</summary>
    public List<ValidationIssue> Issues { get; set; } = new();

    /// <summary>是否需要重新生成</summary>
    public bool ShouldRegenerate { get; set; }

    /// <summary>清理后的文本</summary>
    public string SanitizedText { get; set; } = "";
}

/// <summary>
/// 验证问题
/// </summary>
public class ValidationIssue
{
    /// <summary>问题类型</summary>
    public string IssueType { get; set; } = "";

    /// <summary>问题描述</summary>
    public string Description { get; set; } = "";

    /// <summary>严重程度: block/warn</summary>
    public string Severity { get; set; } = "warn";
}
