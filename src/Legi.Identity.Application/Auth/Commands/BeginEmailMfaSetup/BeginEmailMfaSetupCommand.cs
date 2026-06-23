using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.BeginEmailMfaSetup;

/// <summary>
/// Starts email-MFA enrollment by emailing a one-time code to the account address.
/// Enrollment is finished with <c>ConfirmEmailMfaSetup</c>.
/// </summary>
/// <param name="Language">Preferred email language (primary subtag); falls back to English.</param>
public record BeginEmailMfaSetupCommand(Guid UserId, string? Language) : IRequest<Unit>;
