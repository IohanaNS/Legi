using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.DisableMfa;

/// <param name="Code">A current TOTP code or an unused recovery code.</param>
public record DisableMfaCommand(Guid UserId, string Code) : IRequest<Unit>;
