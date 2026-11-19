namespace NewsHub.Application.Features.Notifications.Commands.MarkNotificationAsRead;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record MarkNotificationAsReadCommand(Guid Id) : IRequest;

public sealed class MarkNotificationAsReadCommandValidator : AbstractValidator<MarkNotificationAsReadCommand>
{
    public MarkNotificationAsReadCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Định danh thông báo không được để trống.");
    }
}

public sealed class MarkNotificationAsReadCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để thực hiện thao tác này.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        var notification = await dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == userId, cancellationToken);

        if (notification == null)
        {
            throw NotFoundException.WithErrorCode("Thông báo không tồn tại hoặc bạn không có quyền truy cập.", ErrorCodes.NotificationNotFound);
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
