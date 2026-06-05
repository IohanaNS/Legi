using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Profiles.Queries.SearchUsers;

public class SearchUsersQueryHandler(IUserProfileReadRepository userProfileReadRepository)
    : IRequestHandler<SearchUsersQuery, IReadOnlyList<FollowUserDto>>
{
    public async Task<IReadOnlyList<FollowUserDto>> Handle(
        SearchUsersQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedPrefix = request.UsernamePrefix.Trim().ToLowerInvariant();

        return await userProfileReadRepository.SearchByUsernamePrefixAsync(
            normalizedPrefix,
            request.ViewerUserId,
            request.Limit,
            cancellationToken);
    }
}
