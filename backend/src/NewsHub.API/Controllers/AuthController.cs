namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NewsHub.API.Extensions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Auth.Commands.Login;
using NewsHub.Application.Features.Auth.Commands.Logout;
using NewsHub.Application.Features.Auth.Commands.RefreshToken;
using NewsHub.Application.Features.Auth.Commands.Register;
using NewsHub.Application.Features.Auth.Commands.RegisterJournalist;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider,
    IJwtTokenGenerator jwtTokenGenerator,
    IWebHostEnvironment environment) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private const string AuthCookiePath = "/api/v1/auth";

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(command, cancellationToken);

        return Created("/api/v1/account/me", ApiResponseFactory.Success(result, correlationId));
    }

    [HttpPost("register-journalist")]
    public async Task<IActionResult> RegisterJournalist(
        [FromBody] RegisterJournalistCommand command,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(command, cancellationToken);

        return Created("/api/v1/account/me", ApiResponseFactory.Success(result, correlationId));
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitingExtensions.LoginPolicy)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(command, cancellationToken);

        SetRefreshTokenCookie(result.RawRefreshToken);

        var responseData = new
        {
            accessToken = result.AccessToken,
            tokenType = result.TokenType,
            expiresIn = result.ExpiresIn,
            user = result.User
        };

        return Ok(ApiResponseFactory.Success(responseData, correlationId));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];

        var result = await mediator.Send(new RefreshTokenCommand(rawRefreshToken), cancellationToken);

        SetRefreshTokenCookie(result.RawRefreshToken);

        var responseData = new
        {
            accessToken = result.AccessToken,
            tokenType = result.TokenType,
            expiresIn = result.ExpiresIn
        };

        return Ok(ApiResponseFactory.Success(responseData, correlationId));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var rawRefreshToken = Request.Cookies[RefreshTokenCookieName];

        await mediator.Send(new LogoutCommand(rawRefreshToken), cancellationToken);

        DeleteRefreshTokenCookie();

        return Ok(ApiResponseFactory.Success(new { message = "Đăng xuất thành công." }, correlationId));
    }

    private CookieOptions CreateRefreshTokenCookieOptions(DateTimeOffset? expires = null)
    {
        var isDevelopment = environment.IsDevelopment();
        var isSecure = Request.IsHttps || !isDevelopment ||
                       string.Equals(Request.Headers["X-Forwarded-Proto"], "https", StringComparison.OrdinalIgnoreCase);

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            // Cross-Site (Vercel <-> Render) bắt buộc SameSite=None + Secure=true để trình duyệt gửi cookie.
            // Local Development trên plain HTTP dùng SameSite=Lax để trình duyệt không chặn.
            SameSite = isSecure && !isDevelopment ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = expires,
            Path = AuthCookiePath
        };
    }

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = CreateRefreshTokenCookieOptions(
            DateTimeOffset.UtcNow.AddDays(jwtTokenGenerator.RefreshTokenExpirationDays));

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private void DeleteRefreshTokenCookie()
    {
        var cookieOptions = CreateRefreshTokenCookieOptions();

        Response.Cookies.Delete(RefreshTokenCookieName, cookieOptions);
    }
}
