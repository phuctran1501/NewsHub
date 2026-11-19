namespace NewsHub.Application.Features.Companies.DTOs;

using NewsHub.Domain.Enums;

public sealed record MyCompanyDto(
    Guid Id,
    string Name,
    string Code,
    string TaxCode,
    CompanyMemberRole Role,
    DateTimeOffset JoinedAt
);
