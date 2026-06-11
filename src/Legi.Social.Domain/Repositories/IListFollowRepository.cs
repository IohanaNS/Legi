using Legi.Social.Domain.Entities;

namespace Legi.Social.Domain.Repositories;

public interface IListFollowRepository
{
    Task<ListFollow?> GetByUserAndListAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid listId, CancellationToken cancellationToken = default);
    Task<int> CountByListAsync(Guid listId, CancellationToken cancellationToken = default);
    Task AddAsync(ListFollow follow, CancellationToken cancellationToken = default);
    Task DeleteAsync(ListFollow follow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages deletion of every follow of the given list in the change tracker
    /// without saving. Used when the list is deleted upstream. Loads then removes
    /// via the tracker — never ExecuteDelete — so it commits atomically with the
    /// inbox row (decision 8.1.3). Idempotent: no rows is a no-op.
    /// </summary>
    Task StageDeleteByListAsync(Guid listId, CancellationToken cancellationToken = default);
}
