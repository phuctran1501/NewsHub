namespace NewsHub.Application.Features.Admin.Commands.SendSystemBroadcast;

using FluentValidation;
using MediatR;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

/// <summary>
/// Command phát sóng thông báo khẩn cấp toàn hệ thống tới tất cả người dùng online qua SignalR.
/// </summary>
public sealed record SendSystemBroadcastCommand(
    string Title,
    string Message) : IRequest;

public sealed class SendSystemBroadcastCommandValidator : AbstractValidator<SendSystemBroadcastCommand>
{
    public SendSystemBroadcastCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tiêu đề thông báo không được để trống.")
            .MaximumLength(150).WithMessage("Tiêu đề thông báo không được vượt quá 150 ký tự.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Nội dung thông báo không được để trống.")
            .MaximumLength(500).WithMessage("Nội dung thông báo không được vượt quá 500 ký tự.");
    }
}

public sealed class SendSystemBroadcastCommandHandler(
    INotificationService notificationService,
    ICurrentUser currentUser) : IRequestHandler<SendSystemBroadcastCommand>
{
    public async Task Handle(SendSystemBroadcastCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền phát sóng thông báo hệ thống.", ErrorCodes.PermissionDenied);
        }

        await notificationService.BroadcastAsync(request.Title, request.Message, cancellationToken);
    }
}
