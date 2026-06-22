using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Entities;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    ILoginAttemptRepository loginAttemptRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    LoginLockoutSettings loginLockoutSettings,
    TurnstileSettings turnstileSettings,
    IHumanVerificationService humanVerificationService,
    ISecurityAuditLogger auditLogger)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    private const string InvalidCredentialsMessage = "Invalid credentials";

    public async Task<LoginResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var identifier = LoginAttempt.NormalizeIdentifier(request.EmailOrUsername);
        var now = DateTime.UtcNow;
        var loginAttempt = await loginAttemptRepository.GetByIdentifierAsync(identifier, cancellationToken);

        if (loginAttempt?.IsLockedOut(now) == true)
        {
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.LoginBlockedLockout,
                Identifier: identifier,
                IpAddress: request.RemoteIpAddress));
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (turnstileSettings.Enabled &&
            (loginAttempt?.FailedAttempts ?? 0) >= turnstileSettings.LoginFailedAttemptsBeforeRequired &&
            !await humanVerificationService.VerifyAsync(
                request.TurnstileToken,
                request.RemoteIpAddress,
                HumanVerificationActions.Login,
                cancellationToken))
        {
            throw new HumanVerificationRequiredException();
        }

        var user = await userRepository.GetByEmailOrUsernameAsync(identifier, cancellationToken);

        if (user is null)
        {
            await RecordFailedAttemptAsync(identifier, now, cancellationToken);
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.LoginFailed,
                Identifier: identifier,
                IpAddress: request.RemoteIpAddress,
                Detail: "unknown-user"));
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        // Passwordless (e.g. Google-only) accounts cannot authenticate with a password.
        if (user.PasswordHash is null)
        {
            await RecordFailedAttemptAsync(identifier, now, cancellationToken);
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.LoginFailed,
                UserId: user.Id,
                Identifier: identifier,
                IpAddress: request.RemoteIpAddress,
                Detail: "passwordless-account"));
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            await RecordFailedAttemptAsync(identifier, now, cancellationToken);
            auditLogger.Record(new SecurityAuditEvent(
                SecurityEventType.LoginFailed,
                UserId: user.Id,
                Identifier: identifier,
                IpAddress: request.RemoteIpAddress,
                Detail: "invalid-password"));
            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        await loginAttemptRepository.ClearAsync(identifier, cancellationToken);
        user.RecordSuccessfulLogin(now);

        if (!user.IsEmailConfirmed)
        {
            await userRepository.UpdateAsync(user, cancellationToken);
            throw new EmailConfirmationRequiredException();
        }

        if (user.MfaEnabled)
        {
            // First factor passed: persist the cleared lockout state and return an MFA
            // challenge instead of tokens. Login completes via CompleteMfaLoginCommand.
            await userRepository.UpdateAsync(user, cancellationToken);

            return new LoginResponse(
                user.Id,
                user.Email.Value,
                user.Username.Value,
                Token: string.Empty,
                RefreshToken: string.Empty,
                ExpiresAt: default,
                RefreshTokenExpiresAt: default,
                MfaRequired: true,
                MfaToken: jwtTokenService.GenerateMfaChallengeToken(user));
        }

        var (token, expiresAt) = jwtTokenService.GenerateAccessToken(user);

        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var refreshTokenExpiresAt = jwtTokenService.GetRefreshTokenExpiresAt();
        user.AddRefreshToken(refreshTokenHash, refreshTokenExpiresAt);

        await userRepository.UpdateAsync(user, cancellationToken);

        auditLogger.Record(new SecurityAuditEvent(
            SecurityEventType.LoginSucceeded,
            UserId: user.Id,
            IpAddress: request.RemoteIpAddress));

        return new LoginResponse(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            token,
            refreshToken,
            expiresAt,
            refreshTokenExpiresAt
        );
    }

    private Task RecordFailedAttemptAsync(
        string identifier,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return loginAttemptRepository.RecordFailedAttemptAsync(
            identifier,
            loginLockoutSettings.MaxFailedAttempts,
            loginLockoutSettings.FailureWindow,
            loginLockoutSettings.LockoutDuration,
            utcNow,
            cancellationToken);
    }
}
