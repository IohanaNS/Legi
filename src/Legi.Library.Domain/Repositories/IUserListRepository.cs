using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IUserListRepository
{
    Task<UserList?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(UserList list, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserList list, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserList list, CancellationToken cancellationToken = default);
    Task<int> GetCountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserAndNameAsync(Guid userId, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserList>> GetListsContainingBookAsync(Guid userBookId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes every <c>UserList</c> row for the given user. The cascade-delete
    /// FK on <c>UserListItem</c> takes care of associated items automatically.
    /// Uses a bulk SQL delete — does not call SaveChangesAsync.
    ///
    /// Called by the Library <c>UserDeletedIntegrationEventHandler</c>.
    /// Idempotent: rerunning deletes zero rows the second time.
    /// </summary>
    /// <returns>The number of <c>UserList</c> rows deleted.</returns>
    Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}