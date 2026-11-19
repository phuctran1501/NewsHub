namespace NewsHub.Application.Features.Companies.DTOs;

public sealed record FollowedCompanyDto(
    Guid CompanyId,
    string CompanyName,
    string CompanyCode,
    string? OwnerName,
    int MemberCount,
    DateTimeOffset FollowedAt
);
