using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Enums;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel.Mediator;

namespace Legi.Identity.Application.Auth.Commands.CompleteMfaLogin;

public class CompleteMfaLoginCommandHandler(
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService,
    ITotpService totpService,
    IMfaSecretProtector secretProtector,
    IMfaEmailCodeRepository emailCodeRepository,
    ISecureTokenFactory tokenFactory,
    ISecurityAuditLogger auditLogger)
    : IRequestHandler<CompleteMfaLoginCommand, LoginResponse>
{
    private const string FailureMessage = "Invalid or expired MFA challenge.";

    public async Task<LoginResponse> Handle(CompleteMfaLoginCommand request, CancellationToken cancellationToken)
    {
        var userId = jwtTokenService.ValidateMfaChallengeToken(request.MfaToken);
        if (userId is null)
            throw new UnauthorizedException(FailureMessage);

        var user = await userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null || !user.MfaEnabled)
            throw new UnauthorizedException(FailureMessage);

        var now = DateTime.UtcNow;
        var (verified, usedRecoveryCode) = user.MfaMethod == MfaMethod.Email
            ? await VerifyEmailCodeOrRecoveryCodeAsync(user, request.Code, now, cancellationToken)
            : VerifyTotpOrRecoveryCode(user, request.Code, now);

        if (!verified)
        {
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.MfaChallengeFailed, UserId: user.Id, IpAddress: request.RemoteIpAddress));
            throw new UnauthorizedException(FailureMessage);
        }

        if (usedRecoveryCode)
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.RecoveryCodeUsed, UserId: user.Id, IpAddress: request.RemoteIpAddress));

        var (token, expiresAt) = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var refreshTokenExpiresAt = jwtTokenService.GetRefreshTokenExpiresAt();
        user.AddRefreshToken(refreshTokenHash, refreshTokenExpiresAt);

        await userRepository.UpdateAsync(user, cancellationToken);

        auditLogger.Record(new SecurityAuditEvent(
            SecurityEventType.LoginSucceeded, UserId: user.Id, IpAddress: request.RemoteIpAddress, Detail: "mfa"));

        return new LoginResponse(
            user.Id, user.Email.Value, user.Username.Value,
            token, refreshToken, expiresAt, refreshTokenExpiresAt);
    }

    private (bool Verified, bool UsedRecoveryCode) VerifyTotpOrRecoveryCode(User user, string code, DateTime now)
    {
        if (!string.IsNullOrEmpty(user.TotpSecret) &&
            totpService.VerifyCode(secretProtector.Unprotect(user.TotpSecret), code))
        {
            return (true, false);
        }

        return VerifyRecoveryCode(user, code, now);
    }

    private async Task<(bool Verified, bool UsedRecoveryCode)> VerifyEmailCodeOrRecoveryCodeAsync(
        User user, string code, DateTime now, CancellationToken cancellationToken)
    {
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
}
