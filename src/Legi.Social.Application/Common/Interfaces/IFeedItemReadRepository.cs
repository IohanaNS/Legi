using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Common.Interfaces;

public interface IFeedItemReadRepository
{
    /// <summary>
    /// Gets the feed for a user — activities from people they follow.
    /// Includes real-time LikesCount/CommentsCount via subquery and IsLikedByMe contextual flag.
    /// Query: JOIN feed_items + follows WHERE follower_id = viewerUserId, ORDER BY created_at DESC.
    /// </summary>
    Task<PaginatedList<FeedItemDto>> GetFeedAsync(
        Guid viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific user's activity history (for their profile page).
    /// Same shape as feed but filtered by actor_id = targetUserId.
    /// ViewerUserId is optional (anonymous viewing returns IsLikedByMe = false).
    /// </summary>
    Task<PaginatedList<FeedItemDto>> GetUserActivityAsync(
        Guid targetUserId,
        Guid? viewerUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}