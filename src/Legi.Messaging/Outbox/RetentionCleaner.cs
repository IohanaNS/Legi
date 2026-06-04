using Legi.Messaging.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Legi.Messaging.Outbox;

/// <summary>
/// Pure retention logic (Fase 6 6D.2), separated from the hosted worker so it is
/// integration-testable without a timer. Deletes <b>processed</b> outbox rows and
/// consumed inbox rows older than the cutoff.
///
/// Poison outbox rows (<c>ProcessedAt == null</c> — never successfully published)
/// are intentionally NOT matched, so they survive for manual diagnosis regardless
/// of age.
/// </summary>
public static class RetentionCleaner
{
    public static async Task<(int OutboxDeleted, int InboxDeleted)> CleanupAsync(
        DbContext context, DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        var outboxDeleted = await context.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

        var inboxDeleted = await context.Set<InboxMessage>()
            .Where(m => m.ProcessedAt < cutoffUtc)
            .ExecuteDeleteAsync(cancellationToken);

        return (outboxDeleted, inboxDeleted);
    }
}
