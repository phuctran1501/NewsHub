namespace NewsHub.Application.Features.Companies.DTOs;

public sealed record CompanySummaryDto(
    Guid Id,
    string Name,
    string Code,
    string TaxCode,
    string? OwnerName,
    int MemberCount,
    int PublishedArticlesCount,
    DateTimeOffset CreatedAt
);
