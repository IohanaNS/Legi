using Legi.SharedKernel.Mediator;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public LogoutCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return; // Silent - don't reveal if user exists

        var refreshTokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var token = user.GetValidRefreshToken(refreshTokenHash);

        if (token is not null)
        {
            user.RevokeRefreshToken(refreshTokenHash);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}
