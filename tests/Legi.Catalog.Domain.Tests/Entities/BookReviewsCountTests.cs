using Legi.Catalog.Domain.Tests.Factories;

namespace Legi.Catalog.Domain.Tests.Entities;

public class BookReviewsCountTests
{
    [Fact]
    public void IncrementReviewsCount_RaisesCountByOne()
    {
        var book = BookBuilder.Valid().Build();
        Assert.Equal(0, book.ReviewsCount);

        book.IncrementReviewsCount();
        book.IncrementReviewsCount();

        Assert.Equal(2, book.ReviewsCount);
    }

    [Fact]
    public void DecrementReviewsCount_LowersCountByOne()
    {
        var book = BookBuilder.Valid().Build();
        book.IncrementReviewsCount();
        book.IncrementReviewsCount();

        book.DecrementReviewsCount();

        Assert.Equal(1, book.ReviewsCount);
    }

    [Fact]
    public void DecrementReviewsCount_AtZero_StaysAtZero()
    {
        var book = BookBuilder.Valid().Build();

        book.DecrementReviewsCount();

        Assert.Equal(0, book.ReviewsCount);
    }
}
