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
        var user = await userRepository.GetByEmailOrUsernameAsync(request.EmailOrUsername, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Invalid credentials");

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid credentials");

        var (token, expiresAt) = jwtTokenService.GenerateAccessToken(user);

        var refreshTokenHash = jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(refreshTokenHash, DateTime.UtcNow.AddDays(7));

        await userRepository.UpdateAsync(user, cancellationToken);

        return new LoginResponse(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            token,
            refreshTokenHash,
            expiresAt
        );
    }
}
