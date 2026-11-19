namespace NewsHub.Application.Features.Companies.DTOs;

using NewsHub.Domain.Enums;

public sealed record PendingCompanyAdminDto(
    Guid Id,
    string Name,
    string Code,
    string TaxCode,
    Guid OwnerId,
    string OwnerName,
    string OwnerEmail,
    string? OwnerPressCardNumber,
    CompanyStatus Status,
    DateTimeOffset CreatedAt
);
