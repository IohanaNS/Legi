using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Entities;

/// <summary>
/// Denormalized read model for the activity feed.
/// Each record represents one action (e.g., "Carlos posted progress on Duna").
/// Created by integration event handlers with all data needed for display,
/// so the feed query is a simple SELECT with no joins to UserProfile or ContentSnapshot.
///
/// LikesCount and CommentsCount are NOT stored here — they are queried in real-time
/// from the likes/comments tables (same database, no cross-service call).
///
/// Not an aggregate — no domain logic, no domain events.
/// </summary>
public class Activity
{
    public Guid Id { get; private set; }
    public Guid ActorId { get; private set; }
    public string ActorUsername { get; private set; } = null!;
    public string? ActorAvatarUrl { get; private set; }
    public ActivityType ActivityType { get; private set; }
    
    /// <summary>
    /// The type of content this activity references (Post, Review, List).
    /// Null if the activity type is not interactable (e.g., BookStarted has no content to like).
    /// Used to join with likes/comments for real-time counts in the feed query.
    /// </summary>
    public InteractableType? TargetType { get; private set; }
    
    /// <summary>
    /// The ID of the original content (PostId, ReviewId, ListId, UserBookId, etc.).
    /// </summary>
    public Guid ReferenceId { get; private set; }
    
    public string? BookTitle { get; private set; }
    public string? BookAuthor { get; private set; }
    public string? BookCoverUrl { get; private set; }
    
    /// <summary>
    /// Flexible JSON payload for type-specific data.
    /// Examples:
    ///   ProgressPosted: { "progress": 67, "currentPage": 443, "totalPages": 662, "content": "Great chapter!" }
    ///   BookRated: { "rating": 4.5 }
    ///   BookFinished: { "content": "Amazing book!", "rating": 5.0 }
    /// </summary>
    public string? Content { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public static Activity Create(
        Guid actorId,
        string actorUsername,
        string? actorAvatarUrl,
        ActivityType activityType,
        InteractableType? targetType,
        Guid referenceId,
        string? bookTitle,
        string? bookAuthor,
        string? bookCoverUrl,
        string? data)
    {
        return new Activity
        {
            Id = Guid.NewGuid(),
            ActorId = actorId,
            ActorUsername = actorUsername,
            ActorAvatarUrl = actorAvatarUrl,
            ActivityType = activityType,
            TargetType = targetType,
            ReferenceId = referenceId,
            BookTitle = bookTitle,
            BookAuthor = bookAuthor,
            BookCoverUrl = bookCoverUrl,
            Content = data,
            CreatedAt = DateTime.UtcNow
        };
    }

}