using System.Text.RegularExpressions;

namespace DHY.Game.Core.Services;

/// <summary>
/// 骰子系统服务
/// </summary>
[ApiDescriptionSettings("Game")]
public class DiceService : IDynamicApiController, ITransient
{
    private static readonly Random _random = new();
    private static readonly Regex _diceRegex = new(@"(\d+)d(\d+)([+-]\d+)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// 普通d20投掷
    /// </summary>
    [DisplayName("投掷D20")]
    [HttpPost("rollD20")]
    public DiceRollResult RollD20()
    {
        var roll = RollDie(20);
        var result = new DiceRollResult
        {
            Rolls = new[] { roll },
            FinalRoll = roll,
            IsNatural20 = roll == 20,
            IsNatural1 = roll == 1
        };
        LogDice("D20", $"投掷结果: {roll}" + (result.IsNatural20 ? " ★自然20★" : result.IsNatural1 ? " x自然1x" : ""));
        return result;
    }

    /// <summary>
    /// 优势投掷（投两次取高）
    /// </summary>
    [DisplayName("优势投掷D20")]
    [HttpPost("rollD20WithAdvantage")]
    public DiceRollResult RollD20WithAdvantage()
    {
        var roll1 = RollDie(20);
        var roll2 = RollDie(20);
        var finalRoll = Math.Max(roll1, roll2);
        var result = new DiceRollResult
        {
            Rolls = new[] { roll1, roll2 },
            FinalRoll = finalRoll,
            HasAdvantage = true,
            IsNatural20 = finalRoll == 20,
            IsNatural1 = finalRoll == 1
        };
        LogDice("D20优势", $"投掷: [{roll1}, {roll2}] → 取高={finalRoll}" + (result.IsNatural20 ? " ★自然20★" : result.IsNatural1 ? " x自然1x" : ""));
        return result;
    }

    /// <summary>
    /// 劣势投掷（投两次取低）
    /// </summary>
    [DisplayName("劣势投掷D20")]
    [HttpPost("rollD20WithDisadvantage")]
    public DiceRollResult RollD20WithDisadvantage()
    {
        var roll1 = RollDie(20);
        var roll2 = RollDie(20);
        var finalRoll = Math.Min(roll1, roll2);
        var result = new DiceRollResult
        {
            Rolls = new[] { roll1, roll2 },
            FinalRoll = finalRoll,
            HasDisadvantage = true,
            IsNatural20 = finalRoll == 20,
            IsNatural1 = finalRoll == 1
        };
        LogDice("D20劣势", $"投掷: [{roll1}, {roll2}] → 取低={finalRoll}" + (result.IsNatural20 ? " ★自然20★" : result.IsNatural1 ? " x自然1x" : ""));
        return result;
    }

    /// <summary>
    /// 伤害骰，解析表达式如 "2d6+3"
    /// </summary>
    [DisplayName("投掷伤害骰")]
    [HttpPost("rollDamage")]
    public DamageRollResult RollDamageApi(RollDamageInput input)
    {
        return RollDamage(input.DiceExpression);
    }

    /// <summary>
    /// 伤害骰内部实现
    /// </summary>
    internal DamageRollResult RollDamage(string diceExpression)
    {
        var match = _diceRegex.Match(diceExpression);
        if (!match.Success)
            throw Oops.Oh("无效的骰子表达式: " + diceExpression);

        var diceCount = int.Parse(match.Groups[1].Value);
        var diceSides = int.Parse(match.Groups[2].Value);
        var modifier = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

        var rolls = new int[diceCount];
        var total = 0;
        for (int i = 0; i < diceCount; i++)
        {
            rolls[i] = RollDie(diceSides);
            total += rolls[i];
        }

        total += modifier;

        var result = new DamageRollResult
        {
            Expression = diceExpression,
            Rolls = rolls,
            Modifier = modifier,
            Total = Math.Max(0, total)
        };
        LogDice("伤害骰", $"表达式={diceExpression}, 投掷=[{string.Join(",", rolls)}], 调整值={modifier:+#;-#;0}, 总伤害={result.Total}");
        return result;
    }

    /// <summary>
    /// 内部骰子投掷方法
    /// </summary>
    internal int RollDie(int sides)
    {
        lock (_random)
        {
            return _random.Next(1, sides + 1);
        }
    }

    /// <summary>
    /// 内部D20投掷（带优势/劣势支持）
    /// </summary>
    internal DiceRollResult RollD20Internal(bool hasAdvantage, bool hasDisadvantage)
    {
        if (hasAdvantage && !hasDisadvantage)
            return RollD20WithAdvantage();
        if (hasDisadvantage && !hasAdvantage)
            return RollD20WithDisadvantage();
        return RollD20();
    }

    #region 日志

    private static readonly object _logLock = new();

    /// <summary>
    /// 骰子日志输出（彩色，线程安全）
    /// </summary>
    private static void LogDice(string diceType, string message)
    {
        lock (_logLock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write($"[骰子] ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"[{diceType}] ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(message);
            Console.ForegroundColor = prevColor;
        }
        GameFileLogger.Write($"[骰子][{diceType}]", message);
    }

    #endregion
}

/// <summary>
/// D20投掷结果
/// </summary>
public class DiceRollResult
{
    /// <summary>所有投掷结果</summary>
    public int[] Rolls { get; set; } = Array.Empty<int>();
    /// <summary>最终采用的点数</summary>
    public int FinalRoll { get; set; }
    /// <summary>是否有优势</summary>
    public bool HasAdvantage { get; set; }
    /// <summary>是否有劣势</summary>
    public bool HasDisadvantage { get; set; }
    /// <summary>是否自然20</summary>
    public bool IsNatural20 { get; set; }
    /// <summary>是否自然1</summary>
    public bool IsNatural1 { get; set; }
}

/// <summary>
/// 伤害骰结果
/// </summary>
public class DamageRollResult
{
    /// <summary>骰子表达式</summary>
    public string Expression { get; set; } = "";
    /// <summary>各骰子结果</summary>
    public int[] Rolls { get; set; } = Array.Empty<int>();
    /// <summary>调整值</summary>
    public int Modifier { get; set; }
    /// <summary>总伤害</summary>
    public int Total { get; set; }
}

/// <summary>
/// 伤害骰输入
/// </summary>
public class RollDamageInput
{
    /// <summary>骰子表达式如"2d6+3"</summary>
    public string DiceExpression { get; set; } = "";
}
