namespace NewsHub.Application.Features.Auth.DTOs;

public sealed record LoginResultDto(
    string AccessToken,
    string RawRefreshToken,
    string TokenType,
    int ExpiresIn,
    UserProfileResponseDto User);
