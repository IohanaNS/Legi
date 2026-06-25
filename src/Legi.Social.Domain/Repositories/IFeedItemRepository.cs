using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;

namespace Legi.Social.Domain.Repositories;

public interface IFeedItemRepository
{
    Task<FeedItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(FeedItem feedItem, CancellationToken cancellationToken = default);
    Task DeleteAsync(FeedItem feedItem, CancellationToken cancellationToken = default);
    Task DeleteByReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default);
    Task DeleteByActorAsync(Guid actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new FeedItem in the change tracker without saving. Used by
    /// integration event handlers; the IntegrationEventDispatcher owns the
    /// commit so the inbox row and the feed projection are atomic
    /// (see MESSAGING-ARCHITECTURE-decisions.md, decisions 8.1 / 8.1.1).
    /// </summary>
    Task StageAddAsync(FeedItem feedItem, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages deletion of every FeedItem with the given ReferenceId in the
    /// change tracker without saving. Loads the rows and removes them via the
    /// tracker — never ExecuteDelete — so the delete commits atomically with
    /// the inbox row (decision 8.1.3). Idempotent: no rows is a no-op.
    /// </summary>
    Task StageDeleteByReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-updates ActorUsername for every feed item belonging to <paramref name="actorId"/>
    /// via ExecuteUpdateAsync (immediate SQL, outside the change tracker).
    /// Idempotent — safe to replay if the integration event is redelivered.
    /// </summary>
    Task BulkUpdateActorUsernameAsync(Guid actorId, string newUsername, CancellationToken cancellationToken = default);
}
