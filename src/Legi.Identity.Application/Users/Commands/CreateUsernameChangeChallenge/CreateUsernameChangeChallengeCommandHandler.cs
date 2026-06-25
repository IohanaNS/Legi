using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.CreateUsernameChangeChallenge;

public class CreateUsernameChangeChallengeCommandHandler(
    IUserRepository userRepository,
    ILoginAttemptRepository attemptRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ITotpService totpService,
    IMfaSecretProtector secretProtector,
    IMfaEmailCodeRepository emailCodeRepository,
    ISecureTokenFactory tokenFactory,
    UsernameChangeChallengeSettings lockoutSettings,
    ISecurityAuditLogger auditLogger)
    : IRequestHandler<CreateUsernameChangeChallengeCommand, UsernameChangeChallengeResponse>
{
    private const string FailureMessage = "Username change verification failed.";

    public async Task<UsernameChangeChallengeResponse> Handle(
        CreateUsernameChangeChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        var attemptIdentifier = GetAttemptIdentifier(user.Id);
        var now = DateTime.UtcNow;
        var attempt = await attemptRepository.GetByIdentifierAsync(attemptIdentifier, cancellationToken);

        if (attempt?.IsLockedOut(now) == true)
        {
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.UsernameChangeChallengesFailed,
                UserId: request.UserId,
                IpAddress: request.RemoteIpAddress,
                Detail: "locked-out"));

            throw new UnauthorizedException(FailureMessage);
        }

        if (user.PasswordHash is not null && !VerifyPassword(user, request.Password))
            await FailAsync(request, attemptIdentifier, now, "invalid-password", cancellationToken);

        if (user.MfaEnabled)
        {
            var (verified, usedRecoveryCode) = user.MfaMethod == MfaMethod.Email
                ? await VerifyEmailCodeOrRecoveryCodeAsync(user, request.MfaCode, now, cancellationToken)
                : VerifyTotpOrRecoveryCode(user, request.MfaCode, now);

            if (!verified)
                await FailAsync(request, attemptIdentifier, now, "invalid-mfa", cancellationToken);

            if (usedRecoveryCode)
            {
                await userRepository.UpdateAsync(user, cancellationToken);
                auditLogger.Record(new SecurityAuditEvent(
                    SecurityEventType.RecoveryCodeUsed,
                    UserId: user.Id,
                    IpAddress: request.RemoteIpAddress));
            }
        }

        if (user.PasswordHash is null && !user.MfaEnabled)
            FailWithoutAttempt(request, "no-reauthentication-factor");

        await attemptRepository.ClearAsync(attemptIdentifier, cancellationToken);

        return new UsernameChangeChallengeResponse(
            jwtTokenService.GenerateUsernameChangeChallengeToken(user));
    }

    private bool VerifyPassword(User user, string? password) =>
        !string.IsNullOrWhiteSpace(password) &&
        passwordHasher.Verify(password, user.PasswordHash!);

    private (bool Verified, bool UsedRecoveryCode) VerifyTotpOrRecoveryCode(
        User user,
        string? code,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (false, false);

        if (!string.IsNullOrEmpty(user.TotpSecret) &&
            totpService.VerifyCode(secretProtector.Unprotect(user.TotpSecret), code))
        {
            return (true, false);
        }

        return VerifyRecoveryCode(user, code, now);
    }

    private async Task<(bool Verified, bool UsedRecoveryCode)> VerifyEmailCodeOrRecoveryCodeAsync(
        User user,
        string? code,
        DateTime now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code))
            return (false, false);

        var active = await emailCodeRepository.GetActiveByUserIdAsync(user.Id, cancellationToken);
        if (active is not null && active.IsUsable(now))
        {
            var hash = tokenFactory.Hash(MfaEmailCodeGenerator.Normalize(code));
            if (active.CodeHash == hash)
            {
                active.Consume(now);
                await emailCodeRepository.UpdateAsync(active, cancellationToken);
                return (true, false);
            }

            active.RegisterFailedAttempt(now);
            await emailCodeRepository.UpdateAsync(active, cancellationToken);
        }

        return VerifyRecoveryCode(user, code, now);
    }

    private (bool Verified, bool UsedRecoveryCode) VerifyRecoveryCode(User user, string code, DateTime now)
    {
        var hash = tokenFactory.Hash(MfaRecoveryCodeGenerator.Normalize(code));
        return user.TryConsumeRecoveryCode(hash, now) ? (true, true) : (false, false);
    }

    private async Task FailAsync(
        CreateUsernameChangeChallengeCommand request,
        string attemptIdentifier,
        DateTime utcNow,
        string detail,
        CancellationToken cancellationToken)
    {
        await attemptRepository.RecordFailedAttemptAsync(
            attemptIdentifier,
            lockoutSettings.MaxFailedAttempts,
            lockoutSettings.FailureWindow,
            lockoutSettings.LockoutDuration,
            utcNow,
            cancellationToken);

        FailWithoutAttempt(request, detail);
    }

    private void FailWithoutAttempt(CreateUsernameChangeChallengeCommand request, string detail)
    {
        auditLogger.Record(new SecurityAuditEvent(
            SecurityEventType.UsernameChangeChallengesFailed,
            UserId: request.UserId,
            IpAddress: request.RemoteIpAddress,
            Detail: detail));

        throw new UnauthorizedException(FailureMessage);
    }

    private static string GetAttemptIdentifier(Guid userId) =>
        $"username-change:{userId:N}";
}
