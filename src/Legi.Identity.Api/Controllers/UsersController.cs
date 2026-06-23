using System.Security.Claims;
using Legi.Identity.Api.Security;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Application.Users.Commands.CreateAccountDeletionChallenge;
using Legi.Identity.Application.Users.Commands.DeleteAccount;
using Legi.Identity.Application.Users.Commands.SendAccountDeletionEmailCode;
using Legi.Identity.Application.Users.Queries.GetCurrentUser;
using Legi.Identity.Application.Users.Queries.GetPublicProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/identity/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHostEnvironment _environment;

    public UsersController(IMediator mediator, IHostEnvironment environment)
    {
        _mediator = mediator;
        _environment = environment;
    }   

    /// <summary>
    /// Returns the authenticated user's profile
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(GetCurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GetCurrentUserResponse>> GetMe(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var query = new GetCurrentUserQuery(userId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Deletes the authenticated user's account
    /// </summary>
    [Authorize]
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteMe(
        [FromBody] DeleteAccountRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var command = new DeleteAccountCommand(userId, request?.DeletionToken ?? string.Empty);
        await _mediator.Send(command, cancellationToken);
        RefreshTokenCookie.Delete(Response, _environment);
        return NoContent();
    }

    /// <summary>
    /// Emails a one-time MFA code for account deletion when the user uses email MFA.
    /// </summary>
    [Authorize]
    [HttpPost("me/deletion-email-code")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendDeletionEmailCode(
        [FromBody] SendAccountDeletionEmailCodeRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new SendAccountDeletionEmailCodeCommand(
            GetCurrentUserId(),
            request?.Language,
            HttpContext.Connection.RemoteIpAddress?.ToString());
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Verifies the current user can perform account deletion and returns a short-lived deletion token.
    /// </summary>
    [Authorize]
    [HttpPost("me/deletion-challenge")]
    [ProducesResponseType(typeof(AccountDeletionChallengeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccountDeletionChallengeResponse>> CreateDeletionChallenge(
        [FromBody] AccountDeletionChallengeRequest? request,
        CancellationToken cancellationToken)
    {
        var command = new CreateAccountDeletionChallengeCommand(
            GetCurrentUserId(),
            request?.Password,
            request?.MfaCode,
            HttpContext.Connection.RemoteIpAddress?.ToString());

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns a user's public profile
    /// </summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(GetPublicProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetPublicProfileResponse>> GetPublicProfile(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserIdOrNull();
        var query = new GetPublicProfileQuery(userId, currentUserId);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }

    private Guid? GetCurrentUserIdOrNull()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;

        return userId;
    }
}

public record AccountDeletionChallengeRequest(string? Password, string? MfaCode);
public record DeleteAccountRequest(string DeletionToken);
public record SendAccountDeletionEmailCodeRequest(string? Language = null);
