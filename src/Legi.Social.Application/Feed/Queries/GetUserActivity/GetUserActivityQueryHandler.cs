using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Feed.Queries.GetUserActivity;

public class GetUserActivityQueryHandler(IFeedItemReadRepository feedItemReadRepository)
    : IRequestHandler<GetUserActivityQuery, PaginatedList<FeedItemDto>>
{
    public async Task<PaginatedList<FeedItemDto>> Handle(
        GetUserActivityQuery request,
        CancellationToken cancellationToken)
    {
        return await feedItemReadRepository.GetUserActivityAsync(
            request.TargetUserId,
            request.ViewerUserId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}