using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.SendMfaEmailCode;

/// <summary>
/// During an email-MFA login, emails a fresh one-time code. Gated by the challenge token
/// returned from the first factor (so only someone who passed the password can request it).
/// Also serves "resend".
/// </summary>
public record SendMfaEmailCodeCommand(string MfaToken, string? Language, string? RemoteIpAddress)
    : IRequest<Unit>;
