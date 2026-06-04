using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Repositories;

public interface IContentSnapshotRepository
{
    Task<ContentSnapshot?> GetByTargetAsync(InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
    Task AddOrUpdateAsync(ContentSnapshot snapshot, CancellationToken cancellationToken = default);
    Task DeleteByTargetAsync(InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages an add (or owner-update of an existing row) in the change tracker
    /// without saving. Used by integration event handlers; the dispatcher owns
    /// the commit (decision 8.1).
    /// </summary>
    Task StageAddOrUpdateAsync(ContentSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages deletion of the snapshot for (targetType, targetId) in the change
    /// tracker without saving. Loads then removes via the tracker — never
    /// ExecuteDelete — so it commits atomically with the inbox row
    /// (decision 8.1.3). Idempotent: missing row is a no-op.
    /// </summary>
    Task StageDeleteByTargetAsync(InteractableType targetType, Guid targetId, CancellationToken cancellationToken = default);
}