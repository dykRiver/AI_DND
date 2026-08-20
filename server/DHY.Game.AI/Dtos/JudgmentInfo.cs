using Newtonsoft.Json;

namespace DHY.Game.AI.Dtos;

/// <summary>
/// 判定信息（由分类AI输出，代码层掷骰后传给导演AI）
/// </summary>
public class JudgmentInfo
{
    /// <summary>是否需要判定</summary>
    [JsonProperty("needed")]
    public bool Needed { get; set; }

    /// <summary>技能名</summary>
    [JsonProperty("skill")]
    public string? Skill { get; set; }

    /// <summary>难度等级（needed=false 时模型可能输出 null，须容忍）</summary>
    [JsonProperty("dc")]
    public int? Dc { get; set; }

    /// <summary>是否有优势</summary>
    [JsonProperty("advantage")]
    public bool Advantage { get; set; }

    /// <summary>是否有劣势</summary>
    [JsonProperty("disadvantage")]
    public bool Disadvantage { get; set; }

    /// <summary>判定原因</summary>
    [JsonProperty("context")]
    public string? Context { get; set; }
}
