namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user rates a book (initial rating or rating change).
/// Consumers:
///   - Catalog (Phase 5): recalculates <c>average_rating</c> and
///     <c>ratings_count</c> on the <c>Book</c>.
///   - Social (Phase 4): creates a <c>FeedItem</c> (BookRated).
///
/// Rating values are integers in [1, 10] (half-stars). <see cref="PreviousRating"/>
/// is null when this is the first rating the user has set for the book.
/// </summary>
public sealed record UserBookRatedIntegrationEvent(
    Guid BookId,
    Guid UserId,
    int Rating,
    int? PreviousRating
) : IIntegrationEvent;
