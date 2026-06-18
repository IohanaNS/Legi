namespace Legi.Contracts.Library;

/// <summary>
/// Published when a user deletes a reading post. Social consumes this to purge
/// the <c>ContentSnapshot</c>, <c>FeedItem</c>, and any associated
/// <c>Like</c>/<c>Comment</c> rows. Catalog consumes it to decrement
/// <c>Book.ReviewsCount</c> when the deleted post was a review.
/// </summary>
public sealed record ReadingPostDeletedIntegrationEvent(
    Guid PostId,
    Guid UserId,
    Guid BookId = default,
    Guid WorkId = default,
    bool IsReview = false
) : IIntegrationEvent;
