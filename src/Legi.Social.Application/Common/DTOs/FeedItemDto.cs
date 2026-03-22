namespace Legi.Social.Application.Common.DTOs;

public class FeedItemDto
{
    public Guid Id { get; init; }
    public Guid ActorId { get; init; }
    public string ActorUsername { get; init; } = null!;
    public string? ActorAvatarUrl { get; init; }
    public string ActivityType { get; init; } = null!;
    
    /// <summary>
    /// Null for non-interactable activity types (e.g., BookStarted).
    /// </summary>
    public string? TargetType { get; init; }
    
    public Guid ReferenceId { get; init; }
    public string? BookTitle { get; init; }
    public string? BookAuthor { get; init; }
    public string? BookCoverUrl { get; init; }
    
    /// <summary>
    /// JSON payload with type-specific data (progress, rating, post content).
    /// </summary>
    public string? Data { get; init; }
    
    // Real-time counts (subquery, not denormalized)
    public int LikesCount { get; init; }
    public int CommentsCount { get; init; }
    
    /// <summary>
    /// Contextual flag — true if the current viewer has liked this content.
    /// Always false for unauthenticated requests or non-interactable items.
    /// </summary>
    public bool IsLikedByMe { get; init; }
    
    public DateTime CreatedAt { get; init; }
}