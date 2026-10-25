using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using NewsHub.Application.Common.Interfaces;

namespace NewsHub.API.Hubs;

/// <summary>
/// Strongly-typed SignalR Hub quản lý kết nối và phát thông báo thời gian thực tới Client.
/// Hỗ trợ cả người dùng đã đăng nhập (định danh qua JWT) lẫn khách vãng lai.
/// </summary>
public class NotificationHub(ILogger<NotificationHub> logger) : Hub<INotificationClient>
{
    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            logger.LogInformation("Client {ConnectionId} đã kết nối SignalR với UserId: {UserId}", connectionId, userId);

            // Tự động đưa vào group theo Role (ví dụ: Role_Journalist, Role_Admin)
            var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
            if (!string.IsNullOrEmpty(role))
            {
                await Groups.AddToGroupAsync(connectionId, $"Role_{role}");
            }
        }
        else
        {
            logger.LogInformation("Khách vãng lai {ConnectionId} đã kết nối SignalR", connectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            logger.LogWarning(exception, "Client {ConnectionId} ngắt kết nối với lỗi: {ErrorMessage}",
                Context.ConnectionId, exception.Message);
        }
        else
        {
            logger.LogInformation("Client {ConnectionId} đã ngắt kết nối SignalR", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Cho phép client tham gia theo dõi thông báo của một bài viết cụ thể.
    /// </summary>
    public async Task JoinArticleRoom(Guid articleId)
    {
        var roomName = $"Article_{articleId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
        logger.LogDebug("Client {ConnectionId} đã tham gia phòng {RoomName}", Context.ConnectionId, roomName);
    }

    /// <summary>
    /// Rời khỏi phòng theo dõi bài viết.
    /// </summary>
    public async Task LeaveArticleRoom(Guid articleId)
    {
        var roomName = $"Article_{articleId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
        logger.LogDebug("Client {ConnectionId} đã rời phòng {RoomName}", Context.ConnectionId, roomName);
    }
}
