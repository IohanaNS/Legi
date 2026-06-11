using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Lists.Queries.GetFollowedLists;

public class GetFollowedListsQueryHandler(IListSocialReadRepository readRepository)
    : IRequestHandler<GetFollowedListsQuery, PaginatedList<FollowedListDto>>
{
    public Task<PaginatedList<FollowedListDto>> Handle(
        GetFollowedListsQuery request,
        CancellationToken cancellationToken)
    {
        return readRepository.GetFollowedListsAsync(
            request.UserId, request.Page, request.PageSize, cancellationToken);
    }
}
