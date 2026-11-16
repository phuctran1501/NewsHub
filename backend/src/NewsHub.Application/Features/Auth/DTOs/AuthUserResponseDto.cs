namespace NewsHub.Application.Features.Auth.DTOs;

using NewsHub.Domain.Enums;

public sealed record AuthUserResponseDto(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    DateTimeOffset CreatedAt);
