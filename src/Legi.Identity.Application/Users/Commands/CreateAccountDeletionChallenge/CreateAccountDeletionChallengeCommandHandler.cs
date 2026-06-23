using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Users.Commands.CreateAccountDeletionChallenge;

public class CreateAccountDeletionChallengeCommandHandler(
    IUserRepository userRepository,
    ILoginAttemptRepository accountDeletionAttemptRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    ITotpService totpService,
    IMfaSecretProtector secretProtector,
    IMfaEmailCodeRepository emailCodeRepository,
    ISecureTokenFactory tokenFactory,
    AccountDeletionChallengeLockoutSettings lockoutSettings,
    ISecurityAuditLogger auditLogger)
    : IRequestHandler<CreateAccountDeletionChallengeCommand, AccountDeletionChallengeResponse>
{
    private const string FailureMessage = "Account deletion verification failed.";

    public async Task<AccountDeletionChallengeResponse> Handle(
        CreateAccountDeletionChallengeCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);
        var attemptIdentifier = GetAttemptIdentifier(user.Id);
        var now = DateTime.UtcNow;
        var attempt = await accountDeletionAttemptRepository.GetByIdentifierAsync(
            attemptIdentifier,
            cancellationToken);

        if (attempt?.IsLockedOut(now) == true)
        {
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.AccountDeletionChallengeFailed,
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

        await accountDeletionAttemptRepository.ClearAsync(attemptIdentifier, cancellationToken);

        return new AccountDeletionChallengeResponse(
            jwtTokenService.GenerateAccountDeletionChallengeToken(user));
    }

    private bool VerifyPassword(User user, string? password)
    {
        return !string.IsNullOrWhiteSpace(password) &&
               passwordHasher.Verify(password, user.PasswordHash!);
    }

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
        CreateAccountDeletionChallengeCommand request,
        string attemptIdentifier,
        DateTime utcNow,
        string detail,
        CancellationToken cancellationToken)
    {
        await accountDeletionAttemptRepository.RecordFailedAttemptAsync(
            attemptIdentifier,
            lockoutSettings.MaxFailedAttempts,
            lockoutSettings.FailureWindow,
            lockoutSettings.LockoutDuration,
            utcNow,
            cancellationToken);

        FailWithoutAttempt(request, detail);
    }

    private void FailWithoutAttempt(CreateAccountDeletionChallengeCommand request, string detail)
    {
        auditLogger.Record(new SecurityAuditEvent(
            SecurityEventType.AccountDeletionChallengeFailed,
            UserId: request.UserId,
            IpAddress: request.RemoteIpAddress,
            Detail: detail));

        throw new UnauthorizedException(FailureMessage);
    }

    private static string GetAttemptIdentifier(Guid userId)
    {
        return $"account-deletion:{userId:N}";
    }
}
