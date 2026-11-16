namespace NewsHub.Application.Features.Media.Commands.DeleteMediaImage;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record DeleteMediaImageCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteMediaImageCommandValidator : AbstractValidator<DeleteMediaImageCommand>
{
    public DeleteMediaImageCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Định danh tệp tin hình ảnh không được để trống.");
    }
}

public sealed class DeleteMediaImageCommandHandler(
    ICurrentUser currentUser,
    ICloudinaryService cloudinaryService,
    IApplicationDbContext dbContext) : IRequestHandler<DeleteMediaImageCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteMediaImageCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Người dùng chưa đăng nhập hoặc phiên làm việc đã hết hạn.", ErrorCodes.Unauthorized);
        }

        var media = await dbContext.MediaFiles
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (media is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tệp tin hình ảnh.", ErrorCodes.MediaNotFound);
        }

        var isOwner = media.UploadedByUserId == currentUser.Id.Value;
        var isAdmin = currentUser.IsInRole(UserRole.Admin);

        if (!isOwner && !isAdmin)
        {
            throw new ForbiddenException("Bạn không có quyền xóa tệp tin hình ảnh này.", ErrorCodes.PermissionDenied);
        }

        var isUsedAsAvatar = await dbContext.Users
            .AnyAsync(u => u.AvatarUrl == media.Url, cancellationToken);

        if (isUsedAsAvatar)
        {
            throw new ConflictException("Tệp tin hình ảnh đang được sử dụng làm ảnh đại diện và không thể xóa.", ErrorCodes.MediaInUse);
        }

        var deleted = await cloudinaryService.DeleteImageAsync(media.PublicId, cancellationToken);
        if (!deleted)
        {
            throw new BadRequestException("Không thể xóa tệp tin hình ảnh trên máy chủ lưu trữ đám mây. Vui lòng thử lại sau.", ErrorCodes.MediaDeleteFailed);
        }

        // Secondary check: Nếu xảy ra race-condition có user gán ảnh làm avatar trong lúc xóa Cloudinary,
        // xóa liên kết AvatarUrl để tránh lỗi vỡ ảnh (Broken Avatar)
        var affectedUsers = await dbContext.Users
            .Where(u => u.AvatarUrl == media.Url)
            .ToListAsync(cancellationToken);

        foreach (var user in affectedUsers)
        {
            user.AvatarUrl = null;
        }

        dbContext.MediaFiles.Remove(media);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
