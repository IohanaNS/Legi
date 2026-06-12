using System.Security.Claims;
using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Auth.Commands.Logout;
using Legi.Identity.Application.Auth.Commands.RefreshToken;
using Legi.Identity.Application.Auth.Commands.Register;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Api.Security;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/identity/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHostEnvironment _environment;

    public AuthController(IMediator mediator, IHostEnvironment environment)
    {
        _mediator = mediator;
        _environment = environment;
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AuthSessionResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(request.Email, request.Username, request.Password);
        var result = await _mediator.Send(command, cancellationToken);

        RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt, _environment);

        return StatusCode(StatusCodes.Status201Created, new AuthSessionResponse(
            result.UserId,
            result.Email,
            result.Username,
            result.Token,
            result.ExpiresAt));
    }

    /// <summary>
    /// Authenticates an existing user
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.EmailOrUsername, request.Password);
        var result = await _mediator.Send(command, cancellationToken);

        RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt, _environment);

        return Ok(new AuthSessionResponse(
            result.UserId,
            result.Email,
            result.Username,
            result.Token,
            result.ExpiresAt));
    }

    /// <summary>
    /// Refreshes access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionResponse>> Refresh(CancellationToken cancellationToken)
    {
        var refreshToken = Request.Cookies[RefreshTokenCookie.Name];
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command, cancellationToken);

        RefreshTokenCookie.Append(Response, result.RefreshToken, result.RefreshTokenExpiresAt, _environment);

        return Ok(new AuthSessionResponse(
            result.UserId,
            result.Email,
            result.Username,
            result.Token,
            result.ExpiresAt));
    }

    /// <summary>
    /// Invalidates the refresh token (logout)
    /// </summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var refreshToken = Request.Cookies[RefreshTokenCookie.Name];

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var command = new LogoutCommand(userId, refreshToken);
            await _mediator.Send(command, cancellationToken);
        }

        RefreshTokenCookie.Delete(Response, _environment);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }
}

// Request DTOs
public record RegisterRequest(string Email, string Username, string Password);
public record LoginRequest(string EmailOrUsername, string Password);
public record AuthSessionResponse(Guid UserId, string Email, string Username, string Token, DateTime ExpiresAt);
