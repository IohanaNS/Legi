using Legi.SharedKernel;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Events;

public sealed class ContentLikedDomainEvent(Guid userId, InteractableType targetType, Guid targetId) : IDomainEvent
{
    public Guid UserId { get; } = userId;
    public InteractableType TargetType { get; } = targetType;
    public Guid TargetId { get; } = targetId;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}