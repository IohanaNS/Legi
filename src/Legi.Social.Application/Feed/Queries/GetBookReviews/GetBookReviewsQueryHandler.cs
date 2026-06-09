using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Feed.Queries.GetBookReviews;

public class GetBookReviewsQueryHandler(IFeedItemReadRepository feedItemReadRepository)
    : IRequestHandler<GetBookReviewsQuery, PaginatedList<FeedItemDto>>
{
    public async Task<PaginatedList<FeedItemDto>> Handle(
        GetBookReviewsQuery request,
        CancellationToken cancellationToken)
    {
        return await feedItemReadRepository.GetBookReviewsAsync(
            request.BookId,
            request.ViewerUserId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
