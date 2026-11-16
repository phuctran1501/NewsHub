namespace NewsHub.Application.Features.Auth.DTOs;

public sealed record RefreshTokenResultDto(
    string AccessToken,
    string RawRefreshToken,
    string TokenType,
    int ExpiresIn);
