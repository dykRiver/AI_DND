namespace DHY.Game.Hub.Options;

/// <summary>
/// 游戏SignalR配置选项
/// </summary>
public class GameSignalROptions : IConfigurableOptions
{
    /// <summary>
    /// 流式推送块延迟毫秒
    /// </summary>
    public int StreamChunkDelayMs { get; set; }

    /// <summary>
    /// 连接超时时间(秒)
    /// </summary>
    public int ConnectionTimeoutSeconds { get; set; }

    /// <summary>
    /// 心跳保活间隔(秒)
    /// </summary>
    public int KeepAliveIntervalSeconds { get; set; }

    /// <summary>
    /// 最大重连尝试次数
    /// </summary>
    public int MaxReconnectAttempts { get; set; }
}
