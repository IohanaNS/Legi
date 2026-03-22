using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Follows.Queries.GetFollowers;

public record GetFollowersQuery(
    Guid UserId,
    Guid? ViewerUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<FollowUserDto>>;