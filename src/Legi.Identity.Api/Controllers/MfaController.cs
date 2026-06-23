using System.Security.Claims;
using Legi.Identity.Application.Auth.Commands.BeginEmailMfaSetup;
using Legi.Identity.Application.Auth.Commands.BeginMfaSetup;
using Legi.Identity.Application.Auth.Commands.ConfirmEmailMfaSetup;
using Legi.Identity.Application.Auth.Commands.ConfirmMfaSetup;
using Legi.Identity.Application.Auth.Commands.DisableMfa;
using Legi.SharedKernel.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Legi.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/identity/mfa")]
[Authorize]
public class MfaController : ControllerBase
{
    private readonly IMediator _mediator;

    public MfaController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Begins TOTP enrollment: returns the secret and otpauth URI for the authenticator app.
    /// </summary>
    [HttpPost("setup")]
    [ProducesResponseType(typeof(BeginMfaSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BeginMfaSetupResponse>> Setup(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new BeginMfaSetupCommand(GetUserId()), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Confirms enrollment with a code from the authenticator and returns one-time recovery codes.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmMfaSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConfirmMfaSetupResponse>> Confirm(
        [FromBody] MfaCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConfirmMfaSetupCommand(GetUserId(), request.Code), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Begins email-code MFA enrollment: emails a one-time code to the account address.
    /// </summary>
    [HttpPost("email/setup")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> EmailSetup(
        [FromBody] EmailMfaSetupRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new BeginEmailMfaSetupCommand(GetUserId(), request.Language), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Confirms email-code enrollment with the emailed code and returns one-time recovery codes.
    /// </summary>
    [HttpPost("email/confirm")]
    [ProducesResponseType(typeof(ConfirmEmailMfaSetupResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ConfirmEmailMfaSetupResponse>> EmailConfirm(
        [FromBody] MfaCodeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new ConfirmEmailMfaSetupCommand(GetUserId(), request.Code), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Disables MFA. Requires a current TOTP code or an unused recovery code.
    /// </summary>
    [HttpPost("disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Disable(
        [FromBody] MfaCodeRequest request,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DisableMfaCommand(GetUserId(), request.Code), cancellationToken);
        return NoContent();
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException();

        return userId;
    }
}

public record MfaCodeRequest(string Code);
public record EmailMfaSetupRequest(string? Language = null);
