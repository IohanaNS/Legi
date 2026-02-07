// Application/Auth/Commands/Login/LoginCommandHandler.cs

using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Auth.Commands.Login;

public class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginCommand, LoginResponse>
{
    public async Task<LoginResponse> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find user
        var user = await userRepository.GetByEmailOrUsernameAsync(request.EmailOrUsername, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Invalid credentials");

        // 2. Verify password
        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials");

        // 3. Generate JWT
        var (token, expiresAt) = jwtTokenService.GenerateAccessToken(user);

        // 4. Create and add RefreshToken to User (aggregate)
        var refreshTokenHash = jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(
            refreshTokenHash,
            DateTime.UtcNow.AddDays(7)
        );

        // 5. Persist (saves User with new RefreshToken)
        await userRepository.UpdateAsync(user, cancellationToken);

        // 6. Return response
        return new LoginResponse(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            user.Name,
            user.AvatarUrl,
            token,
            refreshTokenHash,
            expiresAt
        );
    }
}