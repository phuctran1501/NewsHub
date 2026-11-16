namespace NewsHub.Application.Features.Auth.DTOs;

using NewsHub.Domain.Enums;

public sealed record UserProfileResponseDto(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    UserRole Role,
    string? PhoneNumber,
    string? PressCardNumber,
    string? Bio,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
