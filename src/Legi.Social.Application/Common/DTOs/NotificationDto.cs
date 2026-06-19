namespace Legi.Social.Application.Common.DTOs;

public class NotificationDto
{
    public Guid Id { get; init; }
    public Guid ActorId { get; init; }
    public string ActorUsername { get; init; } = null!;
    public string? ActorAvatarUrl { get; init; }

    /// <summary>"Like" or "Comment".</summary>
    public string NotificationType { get; init; } = null!;

    /// <summary>"Post", "Review", or "List" — the kind of content reacted to.</summary>
    public string TargetType { get; init; } = null!;

    /// <summary>Id of the content reacted to — the deep-link target.</summary>
    public Guid TargetId { get; init; }

    /// <summary>Comment text preview; null for like notifications.</summary>
    public string? CommentPreview { get; init; }

    public bool IsRead { get; init; }
    public DateTime? ReadAt { get; init; }
    public DateTime CreatedAt { get; init; }
}
