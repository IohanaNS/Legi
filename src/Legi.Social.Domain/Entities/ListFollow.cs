using Legi.SharedKernel;

namespace Legi.Social.Domain.Entities;

/// <summary>
/// A user following a custom book list. Distinct from <see cref="Follow"/>
/// (user-to-user): a list follow tracks interest in a specific public list.
/// Composite natural key: (UserId, ListId). Hard delete on unfollow.
///
/// No counters/domain events are maintained for list follows in this iteration —
/// the count is queried live and followed lists are not yet surfaced on profiles.
/// </summary>
public class ListFollow : BaseEntity
{
    public Guid UserId { get; private set; }
    public Guid ListId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static ListFollow Create(Guid userId, Guid listId)
    {
        return new ListFollow
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ListId = listId,
            CreatedAt = DateTime.UtcNow
        };
    }
}
