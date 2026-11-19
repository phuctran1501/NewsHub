namespace NewsHub.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Notifications.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record GetUnreadNotificationCountQuery : IRequest<UnreadNotificationCountDto>;

public sealed class GetUnreadNotificationCountQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetUnreadNotificationCountQuery, UnreadNotificationCountDto>
{
    public async Task<UnreadNotificationCountDto> Handle(
        GetUnreadNotificationCountQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xem thông báo.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        var unreadCount = await dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        return new UnreadNotificationCountDto(unreadCount);
    }
}
