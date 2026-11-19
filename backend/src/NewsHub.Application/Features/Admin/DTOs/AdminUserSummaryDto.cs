namespace NewsHub.Application.Features.Admin.DTOs;

using NewsHub.Domain.Enums;

/// <summary>
/// DTO trả về thông tin người dùng trong danh sách quản trị (không để lộ mật khẩu băm).
/// </summary>
public record AdminUserSummaryDto(
    Guid Id,
    string Email,
    string FullName,
    string? AvatarUrl,
    string? PhoneNumber,
    string? PressCardNumber,
    string? Bio,
    UserRole Role,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
