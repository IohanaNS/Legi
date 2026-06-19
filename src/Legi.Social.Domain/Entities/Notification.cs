using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Entities;

/// <summary>
/// Denormalized read model: a single notification telling a content owner that
/// someone liked or commented on their post, review, or list. Created in-process
/// by the like/comment domain-event handlers, with all data needed for display
/// (actor username/avatar are snapshotted, like <see cref="FeedItem"/>), so the
/// list query is a simple SELECT with no joins.
///
/// Not an aggregate — no domain events. Notifications are historical: undoing the
/// originating like/comment does NOT remove the notification.
/// </summary>
public class Notification
{
    public const int MaxCommentPreviewLength = 200;

    public Guid Id { get; private set; }

    /// <summary>The content owner being notified.</summary>
    public Guid RecipientId { get; private set; }

    /// <summary>Who liked/commented.</summary>
    public Guid ActorId { get; private set; }
    public string ActorUsername { get; private set; } = null!;
    public string? ActorAvatarUrl { get; private set; }

    public NotificationType NotificationType { get; private set; }

    /// <summary>The kind of content reacted to (Post, Review, List).</summary>
    public InteractableType TargetType { get; private set; }

    /// <summary>The id of the content reacted to — the frontend deep-link target.</summary>
    public Guid TargetId { get; private set; }

    /// <summary>
    /// First ~200 chars of the comment, for context in the dropdown.
    /// Null for like notifications.
    /// </summary>
    public string? CommentPreview { get; private set; }

    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Notification CreateLike(
        Guid recipientId,
        Guid actorId,
        string actorUsername,
        string? actorAvatarUrl,
        InteractableType targetType,
        Guid targetId)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            ActorId = actorId,
            ActorUsername = actorUsername,
            ActorAvatarUrl = actorAvatarUrl,
            NotificationType = NotificationType.Like,
            TargetType = targetType,
            TargetId = targetId,
            CommentPreview = null,
            IsRead = false,
            ReadAt = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Notification CreateComment(
        Guid recipientId,
        Guid actorId,
        string actorUsername,
        string? actorAvatarUrl,
        InteractableType targetType,
        Guid targetId,
        string? commentPreview)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            RecipientId = recipientId,
            ActorId = actorId,
            ActorUsername = actorUsername,
            ActorAvatarUrl = actorAvatarUrl,
            NotificationType = NotificationType.Comment,
            TargetType = targetType,
            TargetId = targetId,
            CommentPreview = Truncate(commentPreview),
            IsRead = false,
            ReadAt = null,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Marks the notification read. Idempotent.</summary>
    public void MarkAsRead()
    {
        if (IsRead)
            return;

        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    private static string? Truncate(string? value)
    {
        if (value is null || value.Length <= MaxCommentPreviewLength)
            return value;

        return value[..MaxCommentPreviewLength];
    }
}
