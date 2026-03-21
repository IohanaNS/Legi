using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Entities;

/// <summary>
/// Read model with data about interactable content from other bounded contexts.
/// Serves two purposes:
///   1. Authorization — OwnerId enables content owner to delete comments on their content
///   2. Context display — comments page, likes page, and future notifications need to show
///      what the interaction is about without calling other services
///
/// Not an aggregate — no domain logic, no domain events.
/// PK is composite: (TargetType, TargetId).
///
/// Same pattern as BookSnapshot in Library (projection of Catalog data).
///
/// LikesCount and CommentsCount are NOT stored here — they are queried in real-time
/// from the likes/comments tables (same database, no cross-service call needed).
/// </summary>
public class ContentSnapshot
{
    public const int MaxContentPreviewLength = 200;

    public InteractableType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public string OwnerUsername { get; private set; } = null!;
    public string? OwnerAvatarUrl { get; private set; }
    public string? BookTitle { get; private set; }
    public string? BookAuthor { get; private set; }
    public string? BookCoverUrl { get; private set; }

    /// <summary>
    /// First ~200 characters of the post or review content.
    /// Provides context for comments page and future notifications.
    /// </summary>
    public string? ContentPreview { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public static ContentSnapshot Create(
        InteractableType targetType,
        Guid targetId,
        Guid ownerId,
        string ownerUsername,
        string? ownerAvatarUrl,
        string? bookTitle,
        string? bookAuthor,
        string? bookCoverUrl,
        string? contentPreview)
    {
        return new ContentSnapshot
        {
            TargetType = targetType,
            TargetId = targetId,
            OwnerId = ownerId,
            OwnerUsername = ownerUsername,
            OwnerAvatarUrl = ownerAvatarUrl,
            BookTitle = bookTitle,
            BookAuthor = bookAuthor,
            BookCoverUrl = bookCoverUrl,
            ContentPreview = Truncate(contentPreview),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates denormalized owner data when Identity notifies a username/avatar change.
    /// </summary>
    public void UpdateOwner(string ownerUsername, string? ownerAvatarUrl)
    {
        OwnerUsername = ownerUsername;
        OwnerAvatarUrl = ownerAvatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? Truncate(string? value)
    {
        if (value is null || value.Length <= MaxContentPreviewLength)
            return value;

        return value[..MaxContentPreviewLength];
    }
}