using Legi.SharedKernel;

namespace Legi.Library.Domain.Entities;

public class UserListItem : BaseEntity
{
    public Guid UserBookId { get; private set; }
    public int Order { get; internal set; }
    public DateTime AddedAt { get; private set; }

    internal static UserListItem Create(Guid userBookId, int order)
    {
        return new UserListItem
        {
            Id = Guid.NewGuid(),
            UserBookId = userBookId,
            Order = order,
            AddedAt = DateTime.UtcNow
        };
    }
}