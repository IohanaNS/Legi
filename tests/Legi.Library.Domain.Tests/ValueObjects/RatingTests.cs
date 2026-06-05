using System.Globalization;
using Legi.Library.Domain.Tests.Factories;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;

namespace Legi.Library.Domain.Tests.ValueObjects;

public class RatingTests
{
    [Fact]
    public void Create_ValidHalfStarValue_CreatesRating()
    {
        var rating = Rating.Create(8);

        Assert.Equal(8, rating.Value);
        Assert.Equal(4.0m, rating.Stars);
        Assert.Equal("4.0 stars", rating.ToString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Create_ValueOutsideAllowedRange_ThrowsDomainException(int value)
    {
        Assert.Throws<DomainException>(() => Rating.Create(value));
    }

    [Theory]
    [InlineData("0.5", 1)]
    [InlineData("4.5", 9)]
    [InlineData("5.0", 10)]
    public void FromStars_HalfStarIncrement_CreatesRating(string starsValue, int expectedValue)
    {
        var rating = Rating.FromStars(decimal.Parse(starsValue, CultureInfo.InvariantCulture));

        Assert.Equal(expectedValue, rating.Value);
    }

    [Fact]
    public void FromStars_NonHalfStarIncrement_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => Rating.FromStars(4.25m));
    }

    [Fact]
    public void Equals_SameValue_ReturnsTrue()
    {
        var first = RatingFactory.Create(7);
        var second = RatingFactory.Create(7);

        Assert.Equal(first, second);
    }
}
