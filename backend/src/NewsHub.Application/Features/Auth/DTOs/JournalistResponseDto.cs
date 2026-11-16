namespace NewsHub.Application.Features.Auth.DTOs;

using NewsHub.Domain.Enums;

public sealed record JournalistResponseDto(
    Guid Id,
    string Email,
    string FullName,
    UserRole Role,
    string? PhoneNumber,
    string? PressCardNumber,
    string? Bio,
    DateTimeOffset CreatedAt);
