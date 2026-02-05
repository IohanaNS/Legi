using Legi.SharedKernel;

namespace Legi.Catalog.Domain.Events;

public sealed class BookRatingRecalculatedDomainEvent(
    Guid bookId,
    decimal newAverageRating,
    int totalRatings)
    : IDomainEvent
{
    public Guid BookId { get; } = bookId;
    public decimal NewAverageRating { get; } = newAverageRating;
    public int TotalRatings { get; } = totalRatings;
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}