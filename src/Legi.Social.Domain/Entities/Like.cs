using Legi.SharedKernel;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;

namespace Legi.Social.Domain.Entities;

public class Like : BaseEntity
{
    public Guid UserId { get; private set; }
    public InteractableType TargetType { get; private set; }
    public Guid TargetId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static Like Create(Guid userId, InteractableType targetType, Guid targetId)
    {
        var like = new Like
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TargetType = targetType,
            TargetId = targetId,
            CreatedAt = DateTime.UtcNow
        };

        like.AddDomainEvent(new ContentLikedDomainEvent(userId, targetType, targetId));
        return like;
    }

    /// <summary>
    /// Marks this like for removal, raising the domain event
    /// so the Library can decrement LikesCount on the target content.
    /// The handler is responsible for calling DeleteAsync on the repository.
    /// </summary>
    public void MarkForRemoval()
    {
        AddDomainEvent(new ContentUnlikedDomainEvent(UserId, TargetType, TargetId));
    }
}