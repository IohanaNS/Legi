using Legi.SharedKernel;

namespace Legi.Library.Domain.Events;

/// <summary>
/// Raised when a user writes a book review (a rated, content-only reading post).
/// Translated at the Application boundary into a
/// <c>ReviewCreatedIntegrationEvent</c> for Social (feed fan-out) and Catalog
/// (reviews count). Mirrors <see cref="ReadingProgressCreatedDomainEvent"/>.
/// </summary>
public sealed class ReviewCreatedDomainEvent(
    Guid reviewId,
    Guid userId,
    Guid bookId,
    string content,
    int stars,
    bool isSpoiler = false)
    : IDomainEvent
{
    public Guid ReviewId { get; } = reviewId;
    public Guid UserId { get; } = userId;
    public Guid BookId { get; } = bookId;
    public string Content { get; } = content;

    /// <summary>Half-star rating value in [1, 10].</summary>
    public int Stars { get; } = stars;
    public bool IsSpoiler { get; } = isSpoiler;

    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
