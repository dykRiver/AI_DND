using System.Collections.Concurrent;
using DHY.MG.Module.Sys.Dtos;
using DHY.MG.Module.Sys.Hubs;
using Furion.DataEncryption;
using Furion.InstantMessaging;
using Microsoft.AspNetCore.SignalR;

namespace DHY.Core;

/// <summary>
/// DDBot 专用集线器：采集端(A)和提醒端(B)之间的消息中转
/// 通过 JWT userId 自动配对同一用户的 A 和 B 客户端
/// </summary>
[MapHub("/hubs/ddbot")]
public class DDBotHub : Hub<IDDBotHub>
{
    /// <summary>
    /// 配对信息：按 userId 跟踪 collector 和 reminder 的连接
    /// </summary>
    private class DDBotPairing
    {
        public string? CollectorConnectionId { get; set; }
        public string? ReminderConnectionId { get; set; }
    }

    /// <summary>全局配对表（userId → 配对信息）</summary>
    private static readonly ConcurrentDictionary<long, DDBotPairing> _pairings = new();

    /// <summary>连接ID → userId 的反向映射（用于断开时查找）</summary>
    private static readonly ConcurrentDictionary<string, long> _connectionUserMap = new();

    /// <summary>连接ID → 角色的映射</summary>
    private static readonly ConcurrentDictionary<string, string> _connectionRoleMap = new();

    /// <summary>
    /// 连接建立
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"[DDBotHub] Client connected: {connectionId}");
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// 连接断开：清理配对，通知对端
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connectionId = Context.ConnectionId;
        Console.WriteLine($"[DDBotHub] Client disconnected: {connectionId}, Exception: {exception?.Message}");

        if (_connectionUserMap.TryRemove(connectionId, out var userId))
        {
            _connectionRoleMap.TryRemove(connectionId, out var role);

            if (_pairings.TryGetValue(userId, out var pairing))
            {
                string? peerConnectionId = null;

                if (pairing.CollectorConnectionId == connectionId)
                {
                    pairing.CollectorConnectionId = null;
                    peerConnectionId = pairing.ReminderConnectionId;
                }
                else if (pairing.ReminderConnectionId == connectionId)
                {
                    pairing.ReminderConnectionId = null;
                    peerConnectionId = pairing.CollectorConnectionId;
                }

                // 通知对端离线
                if (!string.IsNullOrEmpty(peerConnectionId))
                {
                    await Clients.Client(peerConnectionId).OnPeerDisconnected(new DDBotPeerInfo
                    {
                        Role = role ?? "",
                        ConnectionId = connectionId
                    });
                }

                // 如果两端都断开，清理配对
                if (pairing.CollectorConnectionId == null && pairing.ReminderConnectionId == null)
                {
                    _pairings.TryRemove(userId, out _);
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// 从 JWT 提取 userId（通过 query string 中的 access_token）
    /// </summary>
    private long GetUserIdFromContext()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext == null) return 0;

        var token = httpContext.Request.Query["access_token"].ToString();
        if (string.IsNullOrEmpty(token)) return 0;

        try
        {
            var claims = JWTEncryption.ReadJwtToken(token)?.Claims;
            var userIdStr = claims?.FirstOrDefault(c => c.Type == ClaimConst.UserId)?.Value;
            return string.IsNullOrWhiteSpace(userIdStr) ? 0 : long.Parse(userIdStr);
        }
        catch
        {
            return 0;
        }
    }

    // ─── 注册方法 ──────────────────────────────────────────

    /// <summary>
    /// 注册为采集端(A)
    /// </summary>
    public async Task RegisterCollector(DDBotClientMetadata metadata)
    {
        var userId = GetUserIdFromContext();
        var connectionId = Context.ConnectionId;

        Console.WriteLine($"[DDBotHub] RegisterCollector: userId={userId}, connId={connectionId}, host={metadata.Hostname}");

        if (userId == 0)
        {
            await Clients.Caller.OnRegistered(new DDBotRegistrationResult
            {
                Success = false, Role = "collector", PairedWith = ""
            });
            return;
        }

        // 记录映射
        _connectionUserMap[connectionId] = userId;
        _connectionRoleMap[connectionId] = "collector";

        var pairing = _pairings.GetOrAdd(userId, _ => new DDBotPairing());
        pairing.CollectorConnectionId = connectionId;

        var pairedWith = pairing.ReminderConnectionId ?? "";

        // 回复注册结果
        await Clients.Caller.OnRegistered(new DDBotRegistrationResult
        {
            Success = true, Role = "collector", PairedWith = pairedWith
        });

        // 如果 reminder 已在线，双向通知
        if (!string.IsNullOrEmpty(pairedWith))
        {
            await Clients.Client(pairedWith).OnPeerConnected(new DDBotPeerInfo
            {
                Role = "collector", ConnectionId = connectionId
            });
            await Clients.Caller.OnPeerConnected(new DDBotPeerInfo
            {
                Role = "reminder", ConnectionId = pairedWith
            });
        }
    }

    /// <summary>
    /// 注册为提醒端(B)
    /// </summary>
    public async Task RegisterReminder(DDBotClientMetadata metadata)
    {
        var userId = GetUserIdFromContext();
        var connectionId = Context.ConnectionId;

        Console.WriteLine($"[DDBotHub] RegisterReminder: userId={userId}, connId={connectionId}, host={metadata.Hostname}");

        if (userId == 0)
        {
            await Clients.Caller.OnRegistered(new DDBotRegistrationResult
            {
                Success = false, Role = "reminder", PairedWith = ""
            });
            return;
        }

        // 记录映射
        _connectionUserMap[connectionId] = userId;
        _connectionRoleMap[connectionId] = "reminder";

        var pairing = _pairings.GetOrAdd(userId, _ => new DDBotPairing());
        pairing.ReminderConnectionId = connectionId;

        var pairedWith = pairing.CollectorConnectionId ?? "";

        // 回复注册结果
        await Clients.Caller.OnRegistered(new DDBotRegistrationResult
        {
            Success = true, Role = "reminder", PairedWith = pairedWith
        });

        // 如果 collector 已在线，双向通知
        if (!string.IsNullOrEmpty(pairedWith))
        {
            await Clients.Client(pairedWith).OnPeerConnected(new DDBotPeerInfo
            {
                Role = "reminder", ConnectionId = connectionId
            });
            await Clients.Caller.OnPeerConnected(new DDBotPeerInfo
            {
                Role = "collector", ConnectionId = pairedWith
            });
        }
    }

    // ─── 消息中转方法 ──────────────────────────────────────

    /// <summary>
    /// A端发送采集消息 → 转发给配对的B端
    /// </summary>
    public async Task SendCollectedMessages(DDBotCollectedBatch batch)
    {
        var reminderConnId = GetPairedConnectionId("reminder");
        if (!string.IsNullOrEmpty(reminderConnId))
        {
            await Clients.Client(reminderConnId).OnCollectedMessages(batch);
        }
    }

    /// <summary>
    /// A端发送状态更新 → 转发给配对的B端
    /// </summary>
    public async Task SendStatusUpdate(DDBotStatusUpdate status)
    {
        var reminderConnId = GetPairedConnectionId("reminder");
        if (!string.IsNullOrEmpty(reminderConnId))
        {
            await Clients.Client(reminderConnId).OnStatusUpdate(status);
        }
    }

    /// <summary>
    /// B端发送控制指令 → 转发给配对的A端
    /// </summary>
    public async Task SendControlCommand(DDBotControlCommand command)
    {
        var collectorConnId = GetPairedConnectionId("collector");
        if (!string.IsNullOrEmpty(collectorConnId))
        {
            await Clients.Client(collectorConnId).OnControlCommand(command);
        }
    }

    /// <summary>
    /// B端发送配置 → 转发给配对的A端
    /// </summary>
    public async Task SendConfig(DDBotConfigSync config)
    {
        var collectorConnId = GetPairedConnectionId("collector");
        if (!string.IsNullOrEmpty(collectorConnId))
        {
            await Clients.Client(collectorConnId).OnConfigSync(config);
        }
    }

    /// <summary>
    /// 心跳检查
    /// </summary>
    public Task<string> Ping()
    {
        return Task.FromResult("pong");
    }

    /// <summary>
    /// 获取连接信息
    /// </summary>
    public Task<object> GetConnectionInfo()
    {
        var connectionId = Context.ConnectionId;
        _connectionRoleMap.TryGetValue(connectionId, out var role);
        _connectionUserMap.TryGetValue(connectionId, out var userId);

        string pairedWith = "";
        if (userId > 0 && _pairings.TryGetValue(userId, out var pairing))
        {
            pairedWith = role == "collector"
                ? pairing.ReminderConnectionId ?? ""
                : pairing.CollectorConnectionId ?? "";
        }

        return Task.FromResult<object>(new
        {
            ConnectionId = connectionId,
            UserId = userId,
            Role = role ?? "",
            PairedWith = pairedWith,
            ServerTime = DateTime.Now
        });
    }

    // ─── 工具方法 ──────────────────────────────────────────

    /// <summary>
    /// 获取当前调用者的配对连接ID
    /// </summary>
    /// <param name="targetRole">目标角色：collector 或 reminder</param>
    private string? GetPairedConnectionId(string targetRole)
    {
        var connectionId = Context.ConnectionId;
        if (!_connectionUserMap.TryGetValue(connectionId, out var userId))
            return null;

        if (!_pairings.TryGetValue(userId, out var pairing))
            return null;

        return targetRole == "collector"
            ? pairing.CollectorConnectionId
            : pairing.ReminderConnectionId;
    }
}
