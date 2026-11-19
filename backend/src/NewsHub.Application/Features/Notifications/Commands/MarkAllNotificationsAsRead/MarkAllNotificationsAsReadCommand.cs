namespace NewsHub.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record MarkAllNotificationsAsReadCommand : IRequest<int>;

public sealed class MarkAllNotificationsAsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<MarkAllNotificationsAsReadCommand, int>
{
    public async Task<int> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để thực hiện thao tác này.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        // Set-based update trực tiếp dưới tầng database mà không cần load các entity vào RAM
        var updatedCount = await dbContext.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTimeOffset.UtcNow), cancellationToken);

        return updatedCount;
    }
}
