using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IReadingPostRepository
{
    Task<ReadingProgress?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(ReadingProgress progress, CancellationToken cancellationToken = default);
    Task UpdateAsync(ReadingProgress progress, CancellationToken cancellationToken = default);
    Task DeleteAsync(ReadingProgress progress, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hard-deletes every <c>ReadingProgress</c> row for the given user.
    /// Uses a bulk SQL delete — does not call SaveChangesAsync.
    ///
    /// Called by the Library <c>UserDeletedIntegrationEventHandler</c>.
    /// Idempotent: rerunning deletes zero rows the second time.
    /// </summary>
    /// <returns>The number of rows deleted.</returns>
    Task<int> DeleteAllForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}