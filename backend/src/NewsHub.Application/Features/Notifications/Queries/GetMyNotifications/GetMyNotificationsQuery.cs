namespace NewsHub.Application.Features.Notifications.Queries.GetMyNotifications;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Notifications.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record GetMyNotificationsQuery(
    int PageNumber = 1,
    int PageSize = 15,
    bool? IsRead = null) : IRequest<PagedList<NotificationItemDto>>;

public sealed class GetMyNotificationsQueryValidator : AbstractValidator<GetMyNotificationsQuery>
{
    public GetMyNotificationsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Số trang (pageNumber) phải lớn hơn 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Kích thước trang (pageSize) phải từ 1 đến 50.");
    }
}

public sealed class GetMyNotificationsQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<GetMyNotificationsQuery, PagedList<NotificationItemDto>>
{
    public async Task<PagedList<NotificationItemDto>> Handle(
        GetMyNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để xem thông báo.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        var query = dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (request.IsRead.HasValue)
        {
            query = query.Where(n => n.IsRead == request.IsRead.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var offset = (long)(request.PageNumber - 1) * request.PageSize;
        if (offset >= totalCount)
        {
            return new PagedList<NotificationItemDto>([], request.PageNumber, request.PageSize, totalCount);
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Skip((int)offset)
            .Take(request.PageSize)
            .Select(n => new NotificationItemDto(
                n.Id,
                n.Type,
                n.Title,
                n.Message,
                n.RelatedEntityId,
                n.TargetUrl,
                n.IsRead,
                n.ReadAt,
                n.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedList<NotificationItemDto>(items, request.PageNumber, request.PageSize, totalCount);
    }
}
