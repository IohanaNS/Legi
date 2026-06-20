using System.Security.Claims;
using Legi.Identity.Application.Auth.Commands.ConfirmEmail;
using Legi.Identity.Application.Auth.Commands.ForgotPassword;
using Legi.Identity.Application.Auth.Commands.GoogleSignIn;
using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Auth.Commands.Logout;
using Legi.Identity.Application.Auth.Commands.RefreshToken;
using Legi.Identity.Application.Auth.Commands.Register;
using Legi.Identity.Application.Auth.Commands.ResendConfirmation;
using Legi.Identity.Application.Auth.Commands.ResetPassword;
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
    [ProducesResponseType(typeof(RegistrationCreatedResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegistrationCreatedResponse>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterCommand(
            request.Email,
            request.Username,
            request.Password,
            request.TurnstileToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.Language);
        var result = await _mediator.Send(command, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new RegistrationCreatedResponse(
            result.UserId,
            result.Email,
            result.Username,
            result.EmailConfirmationRequired));
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
        var command = new LoginCommand(
            request.EmailOrUsername,
            request.Password,
            request.TurnstileToken,
            HttpContext.Connection.RemoteIpAddress?.ToString());
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
    /// Authenticates (or registers) a user with a Google ID token. The same endpoint
    /// handles new and existing accounts.
    /// </summary>
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthSessionResponse>> Google(
        [FromBody] GoogleSignInRequest request,
        CancellationToken cancellationToken)
    {
        var command = new GoogleSignInCommand(
            request.IdToken,
            HttpContext.Connection.RemoteIpAddress?.ToString());
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
    /// Requests a password reset link. Always returns 200 to avoid revealing whether an account exists.
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ForgotPasswordCommand(
            request.Email,
            request.TurnstileToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.Language);
        await _mediator.Send(command, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Resets the password using a token from the reset email.
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResetPasswordCommand(request.Token, request.NewPassword);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Confirms a user's email address using a token from the confirmation email.
    /// </summary>
    [HttpPost("confirm-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(request.Token);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Requests another email confirmation link. Always returns 204 to avoid account enumeration.
    /// </summary>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResendConfirmation(
        [FromBody] ResendConfirmationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ResendConfirmationCommand(
            request.EmailOrUsername,
            request.TurnstileToken,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            request.Language);
        await _mediator.Send(command, cancellationToken);

        return NoContent();
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
public record RegisterRequest(
    string Email,
    string Username,
    string Password,
    string? TurnstileToken = null,
    string? Language = null);
public record LoginRequest(string EmailOrUsername, string Password, string? TurnstileToken = null);
public record GoogleSignInRequest(string IdToken);
public record ForgotPasswordRequest(string Email, string? TurnstileToken = null, string? Language = null);
public record ResetPasswordRequest(string Token, string NewPassword);
public record ConfirmEmailRequest(string Token);
public record ResendConfirmationRequest(string EmailOrUsername, string? TurnstileToken = null, string? Language = null);
public record RegistrationCreatedResponse(Guid UserId, string Email, string Username, bool EmailConfirmationRequired);
public record AuthSessionResponse(Guid UserId, string Email, string Username, string Token, DateTime ExpiresAt);
