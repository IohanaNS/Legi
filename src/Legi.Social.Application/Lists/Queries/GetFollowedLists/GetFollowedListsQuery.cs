using Legi.Social.Application.Common.DTOs;
using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Queries.GetFollowedLists;

public record GetFollowedListsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<FollowedListDto>>;
