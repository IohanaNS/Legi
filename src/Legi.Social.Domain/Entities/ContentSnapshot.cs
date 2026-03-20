using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Entities;

/// <summary>
/// Read model with minimal data about interactable content from other bounded contexts.
/// Provides OwnerId for authorization checks (e.g., content owner can delete comments).
/// Not an aggregate — no domain logic, no domain events.
/// PK is composite: (TargetType, TargetId).
/// 
/// Same pattern as BookSnapshot in Library (projection of Catalog data).
/// </summary>
public class ContentSnapshot
{
    public InteractableType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public Guid OwnerId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static ContentSnapshot Create(
        InteractableType targetType,
        Guid targetId,
        Guid ownerId)
    {
        return new ContentSnapshot
        {
            TargetType = targetType,
            TargetId = targetId,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow
        };
    }
}