using Microsoft.AspNetCore.SignalR;

namespace DHY.Core;

public interface UserIdProvider : IUserIdProvider
{
    public new string GetUserId(HubConnectionContext connection)
    {
        return connection.User?.Claims?.FirstOrDefault(u => u.Type == ClaimConst.UserId)?.Value;
    }
}