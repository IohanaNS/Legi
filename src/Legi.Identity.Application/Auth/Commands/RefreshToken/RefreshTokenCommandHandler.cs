using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Repositories;

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
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);

        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshTokenValue);
        var rotation = await _userRepository.RotateRefreshTokenAsync(
            refreshTokenHash,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(7),
            cancellationToken);

        if (rotation.Status is not RefreshTokenRotationStatus.Success || rotation.User is null)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var (accessToken, expiresAt) = _jwtTokenService.GenerateAccessToken(rotation.User);

        return new RefreshTokenResponse(accessToken, newRefreshTokenValue, expiresAt);
    }
}
