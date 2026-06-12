using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    LoginLockoutSettings loginLockoutSettings,
    TurnstileSettings turnstileSettings,
    IHumanVerificationService humanVerificationService)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    private const string InvalidCredentialsMessage = "Invalid credentials";

    public async Task<LoginResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByEmailOrUsernameAsync(request.EmailOrUsername, cancellationToken);

        if (user is null)
            throw new UnauthorizedException(InvalidCredentialsMessage);

        var now = DateTime.UtcNow;

        if (user.IsLoginLockedOut(now))
            throw new UnauthorizedException(InvalidCredentialsMessage);

        if (turnstileSettings.Enabled &&
            user.FailedLoginAttempts >= turnstileSettings.LoginFailedAttemptsBeforeRequired &&
            !await humanVerificationService.VerifyAsync(
                request.TurnstileToken,
                request.RemoteIpAddress,
                cancellationToken))
        {
            throw new HumanVerificationRequiredException();
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(
                loginLockoutSettings.MaxFailedAttempts,
                loginLockoutSettings.FailureWindow,
                loginLockoutSettings.LockoutDuration,
                now);

            await userRepository.UpdateAsync(user, cancellationToken);

            throw new UnauthorizedException(InvalidCredentialsMessage);
        }

        var (token, expiresAt) = jwtTokenService.GenerateAccessToken(user);

        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var refreshTokenExpiresAt = jwtTokenService.GetRefreshTokenExpiresAt();
        user.RecordSuccessfulLogin(now);
        user.AddRefreshToken(refreshTokenHash, refreshTokenExpiresAt);

        await userRepository.UpdateAsync(user, cancellationToken);

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
}
