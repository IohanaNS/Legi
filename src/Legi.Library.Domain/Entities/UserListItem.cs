using Legi.SharedKernel;

namespace Legi.Library.Domain.Entities;

public class UserListItem : BaseEntity
{
    public Guid BookId { get; private set; }
    public int Order { get; internal set; }
    public DateTime AddedAt { get; private set; }

    internal static UserListItem Create(Guid bookId, int order)
    {
        return new UserListItem
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            Order = order,
            AddedAt = DateTime.UtcNow
        };
    }
}
