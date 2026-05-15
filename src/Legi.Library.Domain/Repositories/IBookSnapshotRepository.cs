using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Repositories;

public interface IBookSnapshotRepository
{
    Task<BookSnapshot?> GetByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a snapshot and commits the change immediately.
    /// Used by command handlers that own their unit of work.
    /// </summary>
    Task AddOrUpdateAsync(BookSnapshot snapshot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a snapshot in the change tracker without saving.
    /// Used by integration event handlers, where the IntegrationEventDispatcher
    /// owns the SaveChangesAsync call so the inbox row commits atomically with
    /// the snapshot. Calling SaveChangesAsync here would break that atomicity —
    /// see MESSAGING-ARCHITECTURE-decisions.md, decision 8.1.
    /// </summary>
    Task StageAddOrUpdateAsync(BookSnapshot snapshot, CancellationToken cancellationToken = default);
}