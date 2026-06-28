using DHY.Game.Core.Logging;

namespace DHY.Game.AI.Utils;

/// <summary>
/// AI调试日志工具 - 控制台彩色输出 + 文件日志，用于测试模式下实时观测AI调用链路
/// 文件日志路径: Logs/game_{date}.log
/// </summary>
public static class AiDebugLogger
{
    private static readonly object _lock = new();

    /// <summary>
    /// 输出AI调用链路日志
    /// </summary>
    public static void LogCallChain(string aiRole, string message)
    {
        WriteColored($"[AI链路][{aiRole}] ", ConsoleColor.Cyan, message, ConsoleColor.White);
        GameFileLogger.Write($"[AI链路][{aiRole}]", message);
    }

    /// <summary>
    /// 输出AI请求信息（控制台+文件，不含SystemPrompt —— 完整上下文由LogFullMessages记录）
    /// </summary>
    public static void LogRequest(string aiRole, string modelId, int messageCount)
    {
        var msg = $"模型={modelId}, 消息数={messageCount}";
        WriteColored($"[AI请求][{aiRole}] ", ConsoleColor.Yellow, msg, ConsoleColor.Gray);
        GameFileLogger.Write($"[AI请求][{aiRole}]", msg);
    }

    /// <summary>
    /// 记录完整的AI调用消息列表到文件（仅文件，不输出控制台）
    /// 用于排查提示词拼接问题
    /// </summary>
    public static void LogFullMessages(string aiRole, List<ChatMessage> messages)
    {
        GameFileLogger.Write($"[AI完整上下文][{aiRole}]", $"消息数={messages.Count}");
        for (int i = 0; i < messages.Count; i++)
        {
            var m = messages[i];
            GameFileLogger.Write($"[AI完整上下文][{aiRole}]", $"--- msg[{i}] role={m.Role} ---");
            GameFileLogger.Write($"[AI完整上下文][{aiRole}]", m.Content ?? "(null)");
        }
        GameFileLogger.WriteSeparator();
    }

    /// <summary>
    /// 输出AI完整响应
    /// </summary>
    public static void LogResponse(string aiRole, string content, int inputTokens, int outputTokens, long durationMs)
    {
        var msg = $"耗时={durationMs}ms, Token=({inputTokens}+{outputTokens})";
        WriteColored($"[AI响应][{aiRole}] ", ConsoleColor.Green, msg, ConsoleColor.Gray);
        WriteColored($"  内容:\n", ConsoleColor.DarkGray, content, ConsoleColor.White);
        WriteSeparator();
        GameFileLogger.Write($"[AI响应][{aiRole}]", msg);
        GameFileLogger.Write($"[AI响应][{aiRole}]", $"内容: {content}");
        GameFileLogger.WriteSeparator();
    }

    /// <summary>
    /// 输出AI流式chunk（实时，仅控制台，不写文件）
    /// </summary>
    public static void LogStreamChunk(string chunk)
    {
        lock (_lock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(chunk);
            Console.ForegroundColor = prevColor;
        }
    }

    /// <summary>
    /// 流式输出结束标记（含完整内容写入文件）
    /// </summary>
    public static void LogStreamEnd(string aiRole, long durationMs, string? fullContent = null)
    {
        var msg = $"耗时={durationMs}ms";
        WriteColored($"\n[AI流式结束][{aiRole}] ", ConsoleColor.Green, msg, ConsoleColor.Gray);
        WriteSeparator();
        GameFileLogger.Write($"[AI流式结束][{aiRole}]", msg);
        if (!string.IsNullOrEmpty(fullContent))
            GameFileLogger.Write($"[AI流式结束][{aiRole}]", $"完整内容: {fullContent}");
        GameFileLogger.WriteSeparator();
    }

    /// <summary>
    /// 流式输出开始标记
    /// </summary>
    public static void LogStreamStart(string aiRole, string modelId, int messageCount)
    {
        var msg = $"模型={modelId}, 消息数={messageCount}";
        WriteColored($"[AI流式开始][{aiRole}] ", ConsoleColor.Yellow, msg, ConsoleColor.Gray);
        GameFileLogger.Write($"[AI流式开始][{aiRole}]", msg);
    }

    /// <summary>
    /// 输出编排层流程日志
    /// </summary>
    public static void LogOrchestration(string step, string detail)
    {
        var msg = $"{step}: {detail}";
        WriteColored($"[编排] ", ConsoleColor.Magenta, msg, ConsoleColor.White);
        GameFileLogger.Write("[编排]", msg);
    }

    /// <summary>
    /// 输出错误信息
    /// </summary>
    public static void LogError(string aiRole, string error)
    {
        WriteColored($"[AI错误][{aiRole}] ", ConsoleColor.Red, error, ConsoleColor.Red);
        GameFileLogger.Write($"[AI错误][{aiRole}]", error);
    }

    private static void WriteColored(string prefix, ConsoleColor prefixColor, string message, ConsoleColor messageColor)
    {
        lock (_lock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = prefixColor;
            Console.Write(prefix);
            Console.ForegroundColor = messageColor;
            Console.WriteLine(message);
            Console.ForegroundColor = prevColor;
        }
    }

    private static void WriteSeparator()
    {
        lock (_lock)
        {
            var prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(new string('─', 60));
            Console.ForegroundColor = prevColor;
        }
    }
}
