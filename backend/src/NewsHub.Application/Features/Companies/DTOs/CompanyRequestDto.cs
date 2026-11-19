namespace NewsHub.Application.Features.Companies.DTOs;

using NewsHub.Domain.Enums;

public sealed record CompanyRequestDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string CompanyCode,
    Guid JournalistId,
    string JournalistName,
    string JournalistEmail,
    string? PressCardNumber,
    CompanyRequestType Type,
    CompanyRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt
);
