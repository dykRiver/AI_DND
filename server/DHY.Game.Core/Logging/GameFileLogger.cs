namespace DHY.Game.Core.Logging;

/// <summary>
/// 游戏关键日志文件写入器
/// 将所有游戏关键控制台日志（AI链路、判定、骰子、编排等）同步写入文件，方便排查问题
/// 日志路径: {AppDomain.CurrentDomain.BaseDirectory}/Logs/game_{yyyy-MM-dd}.log
/// </summary>
public static class GameFileLogger
{
    private static readonly object _lock = new();
    private static readonly string _logDirectory;

    static GameFileLogger()
    {
        _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        if (!Directory.Exists(_logDirectory))
            Directory.CreateDirectory(_logDirectory);
    }

    /// <summary>
    /// 写入一行日志到文件（线程安全）
    /// </summary>
    /// <param name="category">分类标签，如 [骰子][D20]、[判定]、[AI链路][Classifier] 等</param>
    /// <param name="message">日志内容</param>
    public static void Write(string category, string message)
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {category} {message}{Environment.NewLine}";
            var filePath = GetLogFilePath();

            lock (_lock)
            {
                File.AppendAllText(filePath, line);
            }
        }
        catch
        {
            // 文件日志写入失败不应影响业务流程
        }
    }

    /// <summary>
    /// 写入分隔线（用于标记一次完整的行动处理开始/结束）
    /// </summary>
    public static void WriteSeparator()
    {
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {new string('─', 60)}{Environment.NewLine}";
            var filePath = GetLogFilePath();

            lock (_lock)
            {
                File.AppendAllText(filePath, line);
            }
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// 写入空行
    /// </summary>
    public static void WriteLine()
    {
        try
        {
            var filePath = GetLogFilePath();
            lock (_lock)
            {
                File.AppendAllText(filePath, Environment.NewLine);
            }
        }
        catch
        {
            // ignore
        }
    }

    private static string GetLogFilePath()
    {
        return Path.Combine(_logDirectory, $"game_{DateTime.Now:yyyy-MM-dd}.log");
    }
}
