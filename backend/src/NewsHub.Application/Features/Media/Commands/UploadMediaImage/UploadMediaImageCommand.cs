namespace NewsHub.Application.Features.Media.Commands.UploadMediaImage;

using FluentValidation;
using MediatR;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Media.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record UploadMediaImageCommand(
    Stream FileStream,
    string FileName,
    long FileLength,
    string ContentType,
    MediaFolder Folder) : IRequest<MediaFileResponseDto>;

public sealed class UploadMediaImageCommandValidator : AbstractValidator<UploadMediaImageCommand>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp"
    };

    public UploadMediaImageCommandValidator()
    {
        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("Dữ liệu tệp tin không được để trống.");

        RuleFor(x => x.FileLength)
            .GreaterThan(0)
            .WithMessage("Tệp tin hình ảnh không được rỗng.")
            .LessThanOrEqualTo(5 * 1024 * 1024)
            .WithMessage("Dung lượng tệp tin không được vượt quá 5MB.");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .WithMessage("Tên tệp tin không được để trống.")
            .Must(HaveValidImageExtension)
            .WithMessage("Định dạng tệp tin không được hỗ trợ. Chỉ chấp nhận .jpg, .jpeg, .png, .webp.");

        RuleFor(x => x.Folder)
            .IsInEnum()
            .WithMessage("Thư mục lưu trữ hình ảnh không hợp lệ.");
    }

    private static bool HaveValidImageExtension(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var ext = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(ext) && AllowedExtensions.Contains(ext);
    }
}

public sealed class UploadMediaImageCommandHandler(
    ICurrentUser currentUser,
    ICloudinaryService cloudinaryService,
    IApplicationDbContext dbContext) : IRequestHandler<UploadMediaImageCommand, MediaFileResponseDto>
{
    public async Task<MediaFileResponseDto> Handle(
        UploadMediaImageCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || !currentUser.Id.HasValue)
        {
            throw new UnauthorizedException("Người dùng chưa đăng nhập hoặc phiên làm việc đã hết hạn.", ErrorCodes.Unauthorized);
        }

        var uploadResult = await cloudinaryService.UploadImageAsync(
            request.FileStream,
            request.FileName,
            request.Folder,
            cancellationToken);

        var mediaFile = new MediaFile
        {
            PublicId = uploadResult.PublicId,
            Url = uploadResult.Url,
            OriginalFileName = request.FileName,
            Format = uploadResult.Format,
            Bytes = uploadResult.Bytes,
            Width = uploadResult.Width,
            Height = uploadResult.Height,
            Folder = request.Folder,
            UploadedByUserId = currentUser.Id.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.MediaFiles.Add(mediaFile);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new MediaFileResponseDto(
            mediaFile.Id,
            mediaFile.PublicId,
            mediaFile.Url,
            mediaFile.OriginalFileName,
            mediaFile.Format,
            mediaFile.Bytes,
            mediaFile.Width,
            mediaFile.Height,
            mediaFile.Folder,
            mediaFile.UploadedByUserId,
            mediaFile.CreatedAt);
    }
}
