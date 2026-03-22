using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Follows.Queries.GetFollowing;

public class GetFollowingQueryHandler(IFollowReadRepository followReadRepository)
    : IRequestHandler<GetFollowingQuery, PaginatedList<FollowUserDto>>
{
    public async Task<PaginatedList<FollowUserDto>> Handle(
        GetFollowingQuery request,
        CancellationToken cancellationToken)
    {
        return await followReadRepository.GetFollowingAsync(
            request.UserId,
            request.ViewerUserId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}