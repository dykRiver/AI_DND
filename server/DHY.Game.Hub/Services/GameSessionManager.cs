using System.Collections.Concurrent;

namespace DHY.Game.Hub.Services;

/// <summary>
/// 游戏会话连接管理器 (Singleton)
/// 维护 ConnectionId ↔ UserId ↔ SessionId 的映射
/// </summary>
public class GameSessionManager
{
    /// <summary>ConnectionId → UserId</summary>
    private readonly ConcurrentDictionary<string, long> _connectionToUser = new();

    /// <summary>UserId → ConnectionId</summary>
    private readonly ConcurrentDictionary<long, string> _userToConnection = new();

    /// <summary>UserId → ActiveSessionId</summary>
    private readonly ConcurrentDictionary<long, long> _userToSession = new();

    /// <summary>
    /// 注册连接
    /// </summary>
    public void RegisterConnection(string connectionId, long userId)
    {
        _connectionToUser[connectionId] = userId;
        _userToConnection[userId] = connectionId;
    }

    /// <summary>
    /// 移除连接
    /// </summary>
    public void RemoveConnection(string connectionId)
    {
        if (_connectionToUser.TryRemove(connectionId, out var userId))
        {
            _userToConnection.TryRemove(userId, out _);
        }
    }

    /// <summary>
    /// 根据UserId获取ConnectionId
    /// </summary>
    public string? GetConnectionId(long userId)
    {
        return _userToConnection.TryGetValue(userId, out var connectionId) ? connectionId : null;
    }

    /// <summary>
    /// 根据ConnectionId获取UserId
    /// </summary>
    public long? GetUserId(string connectionId)
    {
        return _connectionToUser.TryGetValue(connectionId, out var userId) ? userId : null;
    }

    /// <summary>
    /// 设置用户当前活跃会话
    /// </summary>
    public void SetActiveSession(long userId, long sessionId)
    {
        _userToSession[userId] = sessionId;
    }

    /// <summary>
    /// 获取用户当前活跃会话
    /// </summary>
    public long? GetActiveSession(long userId)
    {
        return _userToSession.TryGetValue(userId, out var sessionId) ? sessionId : null;
    }

    /// <summary>
    /// 移除用户活跃会话
    /// </summary>
    public void RemoveActiveSession(long userId)
    {
        _userToSession.TryRemove(userId, out _);
    }

    /// <summary>
    /// 判断用户是否在线
    /// </summary>
    public bool IsUserOnline(long userId)
    {
        return _userToConnection.ContainsKey(userId);
    }

    /// <summary>
    /// 获取在线用户数
    /// </summary>
    public int GetOnlineUserCount()
    {
        return _userToConnection.Count;
    }
}
