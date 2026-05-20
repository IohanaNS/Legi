using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IUserBookRepository
{
    Task<UserBook?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserBook?> GetByUserAndBookAsync(Guid userId, Guid bookId, CancellationToken cancellationToken = default);
    Task AddAsync(UserBook userBook, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserBook userBook, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes every <c>UserBook</c> row for the given user, including
    /// rows that are currently soft-deleted (the global query filter on
    /// <c>DeletedAt</c> is bypassed). Uses a bulk SQL delete — does not call
    /// SaveChangesAsync.
    ///
    /// Called by the Library <c>UserDeletedIntegrationEventHandler</c>.
    /// Idempotent: rerunning deletes zero rows the second time.
    /// </summary>
    /// <returns>The number of rows deleted.</returns>
    Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}