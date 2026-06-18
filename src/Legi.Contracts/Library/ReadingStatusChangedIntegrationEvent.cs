namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user's reading status for a book changes (e.g. Reading,
/// Finished, Abandoned). Social consumes this and creates a <c>FeedItem</c>
/// (BookFinished) when <see cref="NewStatus"/> is "Finished".
///
/// Statuses are serialized as strings (enum names) so the cross-context
/// contract isn't tied to <c>Legi.Library.Domain.Enums.ReadingStatus</c>.
/// </summary>
public sealed record ReadingStatusChangedIntegrationEvent(
    Guid UserId,
    Guid BookId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt,
    Guid WorkId
) : IIntegrationEvent;
