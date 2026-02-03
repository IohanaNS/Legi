using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Repositories;
using MediatR;

namespace Legi.Identity.Application.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RefreshTokenResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Find User by RefreshToken
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user is null)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        // 2. Validate token
        var currentToken = user.GetValidRefreshToken(request.RefreshToken);

        if (currentToken is null)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        // 3. Revoke current token
        user.RevokeRefreshToken(request.RefreshToken);

        // 4. Create new refresh token
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        user.AddRefreshToken(newRefreshTokenValue, DateTime.UtcNow.AddDays(7));

        // 5. Generate new access token
        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(user);

        // 6. Persist
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 7. Return
        return new RefreshTokenResponse(accessToken, newRefreshTokenValue, expiresAt);
    }
}