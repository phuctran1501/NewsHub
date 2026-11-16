namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Mvc;
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
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
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

    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(7),
            Path = AuthCookiePath
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    private void DeleteRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            Path = AuthCookiePath,
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax
        });
    }
}
