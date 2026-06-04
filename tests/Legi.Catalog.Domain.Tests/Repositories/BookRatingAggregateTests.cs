using Legi.Catalog.Domain.Repositories;

namespace Legi.Catalog.Domain.Tests.Repositories;

public class BookRatingAggregateTests
{
    [Fact]
    public void FromHalfStarRatings_NoRatings_ReturnsZeroAverageAndCount()
    {
        var agg = BookRatingAggregate.FromHalfStarRatings(Array.Empty<int>());

        Assert.Equal(0m, agg.Average);
        Assert.Equal(0, agg.Count);
    }

    [Fact]
    public void FromHalfStarRatings_ConvertsHalfStarMeanToFiveStarScale()
    {
        // two users: 7 and 9 half-stars → mean 8 half-stars → 4.0 on the 0-5 scale
        var agg = BookRatingAggregate.FromHalfStarRatings(new[] { 7, 9 });

        Assert.Equal(4.0m, agg.Average);
        Assert.Equal(2, agg.Count);
    }

    [Fact]
    public void FromHalfStarRatings_SingleRating_HalvesToDisplayValue()
    {
        // one user, 10 half-stars → 5.0; one user, 1 half-star → 0.5
        Assert.Equal(5.0m, BookRatingAggregate.FromHalfStarRatings(new[] { 10 }).Average);
        Assert.Equal(0.5m, BookRatingAggregate.FromHalfStarRatings(new[] { 1 }).Average);
    }

    [Fact]
    public void FromHalfStarRatings_ProducesUnroundedAverage_RoundedDownstreamByBook()
    {
        // 7,8,8 → 23 half-stars / 3 = 7.6667 half → 3.8333 on 0-5 (Book.RecalculateRating rounds to 2dp)
        var agg = BookRatingAggregate.FromHalfStarRatings(new[] { 7, 8, 8 });

        Assert.Equal(3, agg.Count);
        Assert.True(agg.Average > 3.83m && agg.Average < 3.84m, $"expected ~3.833, got {agg.Average}");
        // Book applies the rounding contract:
        Assert.Equal(3.83m, Math.Round(agg.Average, 2));
    }
}
