namespace Legi.Catalog.Infrastructure.Persistence.Entities;

/// <summary>
/// Persistence entity projecting a single user's rating of a book, sourced from
/// the Library service via integration events (Phase 5). NOT a domain entity —
/// it exists only so Catalog can recompute <c>Book.AverageRating</c>/<c>RatingsCount</c>
/// from a complete local source (self-healing / convergent under redelivery).
///
/// Natural key: (BookId, UserId) — a user rates a given book at most once; a
/// re-rate updates <see cref="Rating"/>. <see cref="Rating"/> is the half-star
/// integer (1-10) carried by the events; the 0-5 average conversion happens at
/// recompute time (see <c>BookRatingAggregate</c>).
/// </summary>
public class BookRatingEntity
{
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public DateTime UpdatedAt { get; set; }
}
