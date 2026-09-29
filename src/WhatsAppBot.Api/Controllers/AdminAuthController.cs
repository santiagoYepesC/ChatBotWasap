using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WhatsAppBot.Api.Business.Abstractions;
using WhatsAppBot.Api.Models.Requests;
using WhatsAppBot.Shared.DTOs;
using WhatsAppBot.Shared.Requests;
using WhatsAppBot.Shared.Responses;

namespace WhatsAppBot.Api.Controllers;

[ApiController]
[Route("api/v1/admin/auth")]
public sealed class AdminAuthController(IAdministratorAuthenticator authenticator) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ResponseE<AuthResponseDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseE<object?>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ResponseE<AuthResponseDTO>>> Login(
        [FromBody] LoginRequestDTO request, CancellationToken cancellationToken)
    {
        var response = await authenticator.AuthenticateAsync(
            new AdministratorCredentials(request.Email, request.Password), cancellationToken);
        if (response is null)
        {
            return Unauthorized(ResponseE<AuthResponseDTO>.Fail(
                new ApiErrorDTO { Code = "InvalidCredentials", Message = "Email or password is invalid." },
                HttpContext.TraceIdentifier));
        }

        return Ok(ResponseE<AuthResponseDTO>.Ok(
            new AuthResponseDTO { AccessToken = response.Value, ExpiresAtUtc = response.ExpiresAtUtc },
            HttpContext.TraceIdentifier));
    }

    [Authorize]
    [HttpGet("me")]
    public ActionResult<ResponseE<string>> Me()
    {
        var email = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        return Ok(ResponseE<string>.Ok(email, HttpContext.TraceIdentifier));
    }

}
