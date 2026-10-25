using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace NewsHub.API.Security;

/// <summary>
/// Ánh xạ định danh người dùng trong SignalR từ JWT Claims.
/// Ưu tiên ClaimTypes.NameIdentifier, fallback sang claim "sub" và "id".
/// </summary>
public class CustomUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var user = connection.User;
        if (user == null || user.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? user.FindFirst("id")?.Value;
    }
}
