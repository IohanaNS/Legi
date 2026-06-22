using Legi.Identity.Application.Auth.Commands.Login;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.CompleteMfaLogin;

/// <param name="MfaToken">The challenge token returned by login when MFA is required.</param>
/// <param name="Code">A current TOTP code or an unused recovery code.</param>
public record CompleteMfaLoginCommand(string MfaToken, string Code, string? RemoteIpAddress)
    : IRequest<LoginResponse>;
