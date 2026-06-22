using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.BeginMfaSetup;

public record BeginMfaSetupCommand(Guid UserId) : IRequest<BeginMfaSetupResponse>;

/// <param name="Secret">Base32 secret for manual entry into an authenticator app.</param>
/// <param name="OtpAuthUri">otpauth:// URI to render as a QR code.</param>
public record BeginMfaSetupResponse(string Secret, string OtpAuthUri);
