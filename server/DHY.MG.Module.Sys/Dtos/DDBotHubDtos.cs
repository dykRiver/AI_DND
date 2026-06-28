namespace DHY.MG.Module.Sys.Dtos;

/// <summary>
/// DDBot 采集消息批次（A端 → B端）
/// </summary>
public class DDBotCollectedBatch
{
    /// <summary>批次ID（UUID）</summary>
    public string BatchId { get; set; } = "";

    /// <summary>会话名称</summary>
    public string ConvName { get; set; } = "";

    /// <summary>会话类型：group / private</summary>
    public string ConvType { get; set; } = "group";

    /// <summary>消息列表</summary>
    public List<DDBotRawMessage> Messages { get; set; } = new();
}

/// <summary>
/// DDBot 原始消息项
/// </summary>
public class DDBotRawMessage
{
    /// <summary>消息指纹（MD5去重）</summary>
    public string Fingerprint { get; set; } = "";

    /// <summary>发送者</summary>
    public string Sender { get; set; } = "";

    /// <summary>消息内容</summary>
    public string Content { get; set; } = "";

    /// <summary>消息时间文本</summary>
    public string MsgTime { get; set; } = "";

    /// <summary>消息类型：text / image / file / system</summary>
    public string MsgType { get; set; } = "text";

    /// <summary>是否为回复消息</summary>
    public bool IsReply { get; set; }

    /// <summary>引用消息的发送者</summary>
    public string QuotedSender { get; set; } = "";

    /// <summary>引用消息的内容</summary>
    public string QuotedContent { get; set; } = "";
}

/// <summary>
/// DDBot 采集端状态更新（A端 → B端）
/// </summary>
public class DDBotStatusUpdate
{
    /// <summary>运行状态：running / paused / stopped / error / waiting_config</summary>
    public string State { get; set; } = "stopped";

    /// <summary>已完成的采集周期数</summary>
    public int CycleCount { get; set; }

    /// <summary>上次扫描时间</summary>
    public string LastScanTime { get; set; } = "";

    /// <summary>本次会话总采集消息数</summary>
    public int TotalCollected { get; set; }

    /// <summary>错误信息（仅 state=error 时有值）</summary>
    public string Error { get; set; } = "";
}

/// <summary>
/// DDBot 控制指令（B端 → A端）
/// </summary>
public class DDBotControlCommand
{
    /// <summary>指令：start / pause / stop / resume</summary>
    public string Command { get; set; } = "";
}

/// <summary>
/// DDBot 配置同步（B端 → A端）
/// </summary>
public class DDBotConfigSync
{
    /// <summary>完整配置的JSON字符串</summary>
    public string ConfigJson { get; set; } = "";
}

/// <summary>
/// DDBot 对端连接信息
/// </summary>
public class DDBotPeerInfo
{
    /// <summary>角色：collector / reminder</summary>
    public string Role { get; set; } = "";

    /// <summary>连接ID</summary>
    public string ConnectionId { get; set; } = "";
}

/// <summary>
/// DDBot 注册结果
/// </summary>
public class DDBotRegistrationResult
{
    /// <summary>是否注册成功</summary>
    public bool Success { get; set; }

    /// <summary>注册的角色</summary>
    public string Role { get; set; } = "";

    /// <summary>已配对的对端ConnectionId（无配对时为空）</summary>
    public string PairedWith { get; set; } = "";
}

/// <summary>
/// DDBot 客户端元数据（注册时提交）
/// </summary>
public class DDBotClientMetadata
{
    /// <summary>客户端版本</summary>
    public string ClientVersion { get; set; } = "";

    /// <summary>主机名</summary>
    public string Hostname { get; set; } = "";

    /// <summary>操作系统</summary>
    public string Os { get; set; } = "";
}
