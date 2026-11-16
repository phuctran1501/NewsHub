namespace NewsHub.Application.Features.Media.DTOs;

using NewsHub.Domain.Enums;

public sealed record MediaFileResponseDto(
    Guid Id,
    string PublicId,
    string Url,
    string OriginalFileName,
    string Format,
    long Bytes,
    int Width,
    int Height,
    MediaFolder Folder,
    Guid UploadedByUserId,
    DateTimeOffset CreatedAt);
