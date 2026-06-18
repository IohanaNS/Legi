namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user writes a book review (rating + textual content, no
/// reading progress). A review is an interactable piece of content: Social
/// consumes this to create both a <c>ContentSnapshot</c> (Review) and a
/// <c>FeedItem</c> (ReviewCreated), and Catalog consumes it to maintain
/// <c>Book.ReviewsCount</c>.
///
/// <see cref="Stars"/> is the half-star rating value in [1, 10] (the same scale
/// as <see cref="UserBookRatedIntegrationEvent"/>). The rating itself flows to
/// Catalog's average via <see cref="UserBookRatedIntegrationEvent"/> (flagged
/// <c>IsPartOfReview</c>); this event carries it only for display in the feed.
///
/// Book display data is NOT carried — Social resolves it via its local
/// <c>BookSnapshot</c>. Mirrors <see cref="ReadingPostCreatedIntegrationEvent"/>.
/// </summary>
public sealed record ReviewCreatedIntegrationEvent(
    Guid ReviewId,
    Guid UserId,
    Guid BookId,
    string Content,
    int Stars,
    DateTime CreatedAt,
    Guid WorkId,
    bool IsSpoiler = false
) : IIntegrationEvent;
