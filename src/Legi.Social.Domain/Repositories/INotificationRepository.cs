using Legi.Social.Domain.Entities;

namespace Legi.Social.Domain.Repositories;

public interface INotificationRepository
{
    /// <summary>
    /// Stages a new notification in the change tracker without saving. Called from
    /// the like/comment domain-event handlers, which run inside
    /// <c>SavingChangesAsync</c> (before the commit) — so the notification commits
    /// atomically with the Like/Comment aggregate. No SaveChanges here.
    /// </summary>
    void StageAdd(Notification notification);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a single notification's read state (top-level command path).</summary>
    Task MarkAsReadAsync(Notification notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-marks every currently-unread notification for the recipient as read,
    /// via ExecuteUpdateAsync (immediate, outside the change tracker).
    /// </summary>
    Task MarkAllAsReadAsync(Guid recipientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk-updates ActorUsername for every notification where the actor is <paramref name="actorId"/>
    /// via ExecuteUpdateAsync (immediate SQL, outside the change tracker).
    /// Idempotent — safe to replay if the integration event is redelivered.
    /// </summary>
    Task BulkUpdateActorUsernameAsync(Guid actorId, string newUsername, CancellationToken cancellationToken = default);
}
