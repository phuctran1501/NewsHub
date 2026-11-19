namespace NewsHub.Application.Features.Companies.DTOs;

using NewsHub.Domain.Enums;

public sealed record CompanyMemberDto(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? PressCardNumber,
    CompanyMemberRole Role,
    DateTimeOffset JoinedAt,
    int PublishedArticlesCount
);
