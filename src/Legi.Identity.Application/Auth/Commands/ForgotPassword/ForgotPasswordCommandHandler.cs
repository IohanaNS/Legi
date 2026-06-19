using System.Net;
using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler(
    IUserRepository userRepository,
    ISecureTokenFactory tokenFactory,
    IEmailSender emailSender,
    PasswordResetSettings passwordResetSettings,
    TurnstileSettings turnstileSettings,
    IHumanVerificationService humanVerificationService,
    ILogger<ForgotPasswordCommandHandler> logger)
    : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        if (turnstileSettings.Enabled &&
            turnstileSettings.RequireForPasswordReset &&
            !await humanVerificationService.VerifyAsync(
                request.TurnstileToken,
                request.RemoteIpAddress,
                HumanVerificationActions.PasswordReset,
                cancellationToken))
        {
            throw new HumanVerificationRequiredException();
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailWithPasswordResetTokensAsync(email, cancellationToken);

        if (user is null)
        {
            // Anti-enumeration: respond identically whether or not the account exists.
            logger.LogInformation("Password reset requested for unknown email; no action taken.");
            return Unit.Value;
        }

        var (rawToken, tokenHash) = tokenFactory.Create();
        var expiresAt = DateTime.UtcNow.AddMinutes(passwordResetSettings.TokenLifetimeMinutes);

        user.AddPasswordResetToken(tokenHash, expiresAt);
        await userRepository.UpdateAsync(user, cancellationToken);

        var resetUrl =
            $"{passwordResetSettings.FrontendBaseUrl.TrimEnd('/')}/reset-password?token={WebUtility.UrlEncode(rawToken)}";

        var content = PasswordResetEmailTemplate.Build(
            user.Username.Value,
            resetUrl,
            passwordResetSettings.TokenLifetimeMinutes,
            request.Language);

        await emailSender.SendAsync(user.Email.Value, content, cancellationToken);

        return Unit.Value;
    }
}
