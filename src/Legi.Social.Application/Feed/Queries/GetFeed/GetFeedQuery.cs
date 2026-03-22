using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Feed.Queries.GetFeed;

/// <summary>
/// Gets the authenticated user's feed — activities from people they follow.
/// UserId is always required (feed is a personal view, no anonymous access).
/// </summary>
public record GetFeedQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<FeedItemDto>>;