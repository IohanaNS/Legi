using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Repositories;

public interface ILikeRepository
{
    Task<Like?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Like?> GetByUserAndTargetAsync(Guid userId, InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task AddAsync(Like like, CancellationToken cancellationToken = default);
    Task DeleteAsync(Like like, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages deletion of every Like on (targetType, targetId) in the change
    /// tracker without saving. Used when content is deleted upstream and its
    /// likes must be purged. Loads then removes via the tracker — never
    /// ExecuteDelete — so it commits atomically with the inbox row
    /// (decision 8.1.3). Idempotent: no rows is a no-op.
    /// </summary>
    Task StageDeleteByTargetAsync(InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
}