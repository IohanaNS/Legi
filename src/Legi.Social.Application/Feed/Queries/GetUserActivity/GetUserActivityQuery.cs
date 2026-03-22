using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Feed.Queries.GetUserActivity;

/// <summary>
/// Gets a specific user's activity history for their profile page.
/// ViewerUserId is optional — anonymous visitors see IsLikedByMe = false.
/// </summary>
public record GetUserActivityQuery(
    Guid TargetUserId,
    Guid? ViewerUserId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<FeedItemDto>>;