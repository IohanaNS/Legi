using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ConfirmEmailMfaSetup;

public record ConfirmEmailMfaSetupCommand(Guid UserId, string Code) : IRequest<ConfirmEmailMfaSetupResponse>;

/// <param name="RecoveryCodes">One-time recovery codes — shown to the user once, never again.</param>
public record ConfirmEmailMfaSetupResponse(IReadOnlyList<string> RecoveryCodes);
