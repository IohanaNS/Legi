using Legi.SharedKernel.Mediator;
using Legi.Identity.Domain.Repositories;

namespace Legi.Identity.Application.Users.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, GetCurrentUserResponse>
{
    private readonly IUserRepository _userRepository;

    public GetCurrentUserQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetCurrentUserResponse> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            throw new ApplicationException("USER_NOT_FOUND");

        // Stats will come from other services via events (mock for now)
        var stats = new UserStatsDto(0, 0, 0, 0);

        return new GetCurrentUserResponse(
            user.Id,
            user.Email.Value,
            user.Username.Value,
            user.IsPublicProfile,
            user.CreatedAt,
            stats
        );
    }
}
