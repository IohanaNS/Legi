using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.ConfirmMfaSetup;

public record ConfirmMfaSetupCommand(Guid UserId, string Code) : IRequest<ConfirmMfaSetupResponse>;

/// <param name="RecoveryCodes">One-time recovery codes — shown to the user once, never again.</param>
public record ConfirmMfaSetupResponse(IReadOnlyList<string> RecoveryCodes);
