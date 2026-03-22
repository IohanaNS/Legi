using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Feed.Queries.GetFeed;

public class GetFeedQueryHandler(IFeedItemReadRepository feedItemReadRepository)
    : IRequestHandler<GetFeedQuery, PaginatedList<FeedItemDto>>
{
    public async Task<PaginatedList<FeedItemDto>> Handle(
        GetFeedQuery request,
        CancellationToken cancellationToken)
    {
        return await feedItemReadRepository.GetFeedAsync(
            request.UserId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}