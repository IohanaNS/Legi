using System.Net;
using Legi.Identity.Application.Common.Email;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Auth.Commands.ResendConfirmation;

public class ResendConfirmationCommandHandler(
    IUserRepository userRepository,
    ISecureTokenFactory tokenFactory,
    IEmailSender emailSender,
    EmailConfirmationSettings emailConfirmationSettings,
    TurnstileSettings turnstileSettings,
    IHumanVerificationService humanVerificationService,
    ILogger<ResendConfirmationCommandHandler> logger)
    : IRequestHandler<ResendConfirmationCommand, Unit>
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(3);

    public async Task<Unit> Handle(ResendConfirmationCommand request, CancellationToken cancellationToken)
    {
        if (turnstileSettings.Enabled &&
            turnstileSettings.RequireForEmailConfirmation &&
            !await humanVerificationService.VerifyAsync(
                request.TurnstileToken,
                request.RemoteIpAddress,
                HumanVerificationActions.EmailConfirmation,
                cancellationToken))
        {
            throw new HumanVerificationRequiredException();
        }

        var user = await userRepository.GetByEmailOrUsernameWithEmailConfirmationTokensAsync(
            request.EmailOrUsername,
            cancellationToken);

        if (user is null)
        {
            logger.LogInformation("Email confirmation resend requested for unknown account; no action taken.");
            return Unit.Value;
        }

        if (user.IsEmailConfirmed)
            return Unit.Value;

        var now = DateTime.UtcNow;
        var mostRecentSentToken = user.EmailConfirmationTokens
            .Where(t => t.SentAt.HasValue)
            .OrderByDescending(t => t.SentAt)
            .FirstOrDefault();

        if (mostRecentSentToken?.SentAt?.Add(Cooldown) > now)
            return Unit.Value;

        await CreateTokenAndSendEmailAsync(user, request.Language, now, cancellationToken);

        return Unit.Value;
    }

    private async Task CreateTokenAndSendEmailAsync(
        User user,
        string? language,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var (rawToken, tokenHash) = tokenFactory.Create();
        var expiresAt = utcNow.AddMinutes(emailConfirmationSettings.TokenLifetimeMinutes);

        user.AddEmailConfirmationToken(tokenHash, expiresAt);
        await userRepository.UpdateAsync(user, cancellationToken);

        try
        {
            var confirmationUrl =
                $"{emailConfirmationSettings.FrontendBaseUrl.TrimEnd('/')}/confirm-email?token={WebUtility.UrlEncode(rawToken)}";

            var content = EmailConfirmationEmailTemplate.Build(
                user.Username.Value,
                confirmationUrl,
                emailConfirmationSettings.TokenLifetimeMinutes,
                language);

            await emailSender.SendAsync(user.Email.Value, content, cancellationToken);
            user.MarkEmailConfirmationTokenSent(tokenHash, DateTime.UtcNow);
            await userRepository.UpdateAsync(user, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to send confirmation email for user {UserId}", user.Id);
        }
    }
}
