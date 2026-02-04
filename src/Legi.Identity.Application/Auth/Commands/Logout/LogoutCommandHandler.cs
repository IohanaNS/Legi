using Legi.Identity.Application.Common.Mediator;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Auth.Commands.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
{
    private readonly IUserRepository _userRepository;

    public LogoutCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return; // Silent - don't reveal if user exists

        var token = user.GetValidRefreshToken(request.RefreshToken);

        if (token is not null)
        {
            user.RevokeRefreshToken(request.RefreshToken);
            await _userRepository.UpdateAsync(user, cancellationToken);
        }
    }
}