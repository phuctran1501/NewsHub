namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Auth.Commands.ChangePassword;
using NewsHub.Application.Features.Auth.Commands.UpdateProfile;
using NewsHub.Application.Features.Auth.Queries.GetCurrentUserProfile;

[ApiController]
[Authorize]
[Route("api/v1/account")]
public class AccountController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetCurrentUserProfileQuery(), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileCommand command,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đổi mật khẩu thành công." }, correlationId));
    }
}
