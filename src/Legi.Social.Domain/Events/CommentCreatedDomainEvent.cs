using Legi.SharedKernel;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Events;

public sealed class CommentCreatedDomainEvent(Guid commentId, Guid userId, InteractableType targetType, Guid targetId, string content) : IDomainEvent
{
    public Guid CommentId { get; } = commentId;
    public Guid UserId { get; } = userId;
    public InteractableType TargetType { get; } = targetType;
    public Guid TargetId { get; } = targetId;

    /// <summary>The comment text, carried so the in-process notification handler
    /// can snapshot a preview without an extra DB read.</summary>
    public string Content { get; } = content;

    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}