using Legi.Identity.Application.Auth.Commands.Login;
using Legi.Identity.Application.Common;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Common.Models;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.Identity.Domain.ValueObjects;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Auth.Commands.GoogleSignIn;

public class GoogleSignInCommandHandler(
    IGoogleTokenValidator googleTokenValidator,
    IUserRepository userRepository,
    IJwtTokenService jwtTokenService,
    ISecurityAuditLogger auditLogger,
    ILogger<GoogleSignInCommandHandler> logger)
    : IRequestHandler<GoogleSignInCommand, LoginResponse>
{
    public const string Provider = "google";
    private const int MaxUsernameAttempts = 10;
    private const int MaxCreateAttempts = 3;
    private const string SignInFailedMessage = "Google sign-in failed.";

    public async Task<LoginResponse> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        var info = await googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);

        if (info is null || !info.EmailVerified)
            throw new UnauthorizedException(SignInFailedMessage);

        Email email;
        try
        {
            email = Email.Create(info.Email);
        }
        catch (Legi.SharedKernel.DomainException)
        {
            throw new UnauthorizedException(SignInFailedMessage);
        }

        var now = DateTime.UtcNow;

        var user = await ResolveUserAsync(info, email, now, cancellationToken);

        user.RecordSuccessfulLogin(now);

        var (token, expiresAt) = jwtTokenService.GenerateAccessToken(user);
        var refreshToken = jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = jwtTokenService.HashRefreshToken(refreshToken);
        var refreshTokenExpiresAt = jwtTokenService.GetRefreshTokenExpiresAt();
        user.AddRefreshToken(refreshTokenHash, refreshTokenExpiresAt);

        await userRepository.UpdateAsync(user, cancellationToken);

        auditLogger.Record(new SecurityAuditEvent(
            SecurityEventType.ExternalLoginSucceeded,
            UserId: user.Id,
            IpAddress: request.RemoteIpAddress,
            Detail: Provider));

        return new LoginResponse(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            token,
            refreshToken,
            expiresAt,
            refreshTokenExpiresAt);
    }

    private async Task<User> ResolveUserAsync(
        Common.Models.GoogleUserInfo info,
        Email email,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // 1. Already linked to this Google account.
        var user = await userRepository.GetByExternalLoginAsync(Provider, info.Sub, cancellationToken);
        if (user is not null)
            return user;

        // 2. Existing account with the same (verified) email — auto-link.
        // Unconfirmed accounts are claimed securely (password/sessions revoked) to
        // prevent pre-hijacking; confirmed accounts keep their credential.
        user = await userRepository.GetByEmailAsync(email.Value, cancellationToken);
        if (user is not null)
        {
            user.LinkVerifiedExternalLogin(Provider, info.Sub, now);
            return user;
        }

        // 3. Brand-new user.
        return await CreateUserAsync(info, email, now, cancellationToken);
    }

    private async Task<User> CreateUserAsync(
        Common.Models.GoogleUserInfo info,
        Email email,
        DateTime now,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxCreateAttempts; attempt++)
        {
            var username = await GenerateUniqueUsernameAsync(info, email, cancellationToken);
            var user = User.CreateFromExternalLogin(email, username, Provider, info.Sub, now);

            try
            {
                await userRepository.AddAsync(user, cancellationToken);
                return user;
            }
            catch (ConflictException)
            {
                // A concurrent insert won the race. Resolve by cause:
                // (a) same Google account → use the row it created;
                var bySub = await userRepository.GetByExternalLoginAsync(Provider, info.Sub, cancellationToken);
                if (bySub is not null)
                    return bySub;

                // (b) same email registered meanwhile → claim/link it instead of duplicating;
                var byEmail = await userRepository.GetByEmailAsync(email.Value, cancellationToken);
                if (byEmail is not null)
                {
                    byEmail.LinkVerifiedExternalLogin(Provider, info.Sub, now);
                    return byEmail;
                }

                // (c) username collision → loop and regenerate a fresh username.
                logger.LogInformation("Username collision on Google sign-in for sub {Sub}; retrying", info.Sub);
            }
        }

        throw new ConflictException("Could not create the account. Please try again.");
    }

    private async Task<Username> GenerateUniqueUsernameAsync(
        Common.Models.GoogleUserInfo info,
        Email email,
        CancellationToken cancellationToken)
    {
        var seed = string.IsNullOrWhiteSpace(info.Name) ? email.Value : info.Name;
        var baseName = UsernameGenerator.CreateBase(seed);

        var candidate = baseName;
        for (var attempt = 0; attempt < MaxUsernameAttempts; attempt++)
        {
            var existing = await userRepository.GetByUsernameAsync(candidate, cancellationToken);
            if (existing is null)
                return Username.Create(candidate);

            candidate = UsernameGenerator.WithSuffix(baseName, Random.Shared.Next(1000, 1_000_000));
        }

        // Extremely unlikely; fall back to a near-guaranteed-unique suffix.
        // Mask the sign bit instead of Math.Abs (which throws on int.MinValue).
        candidate = UsernameGenerator.WithSuffix(baseName, Guid.NewGuid().GetHashCode() & int.MaxValue);
        return Username.Create(candidate);
    }
}
