using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Follows.Queries.GetFollowers;

public class GetFollowersQueryHandler(IFollowReadRepository followReadRepository)
    : IRequestHandler<GetFollowersQuery, PaginatedList<FollowUserDto>>
{
    public async Task<PaginatedList<FollowUserDto>> Handle(
        GetFollowersQuery request,
        CancellationToken cancellationToken)
    {
        return await followReadRepository.GetFollowersAsync(
            request.UserId,
            request.ViewerUserId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}