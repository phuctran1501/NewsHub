namespace NewsHub.Application.Features.Ai.DTOs;

public sealed record CategoryCandidateDto(
    Guid Id,
    string Name,
    string Slug);
