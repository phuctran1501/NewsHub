namespace NewsHub.Application.Features.Companies.DTOs;

public sealed record JournalistSearchResultDto(
    Guid UserId,
    string FullName,
    string Email,
    string? AvatarUrl,
    string? PressCardNumber,
    int ActiveCompaniesCount,
    bool IsAlreadyMember,
    bool CanInvite
);
