namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user removes their rating from a book. The sole consumer is
/// Catalog (Phase 5): it deletes the user's <c>BookRating</c> row for the book
/// (located by <see cref="BookId"/> + <see cref="UserId"/>) and recomputes
/// <c>average_rating</c> / <c>ratings_count</c> on the <c>Book</c>.
///
/// <see cref="RemovedRating"/> is the half-star integer (1-10) that was removed.
/// Catalog does NOT read it under the per-user-rows model (the delete is keyed by
/// book + user) — it is carried for traceability/logging and to keep the contract
/// viable if the recompute strategy ever changes. See Phase 5 in
/// MESSAGING-ARCHITECTURE-decisions.md.
/// </summary>
public sealed record UserBookRatingRemovedIntegrationEvent(
    Guid BookId,
    Guid UserId,
    int RemovedRating,
    Guid WorkId
) : IIntegrationEvent;
