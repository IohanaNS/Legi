using Legi.Catalog.Application.Books.Queries.SearchBooks;
using Legi.Catalog.Application.Tests.Factories;

namespace Legi.Catalog.Application.Tests.Books.Queries.SearchBooks;

public class SearchBooksQueryValidatorTests
{
    private readonly SearchBooksQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenPageNumberIsLessThanOne()
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(pageNumber: 0);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Page number must be at least 1");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_ShouldFail_WhenPageSizeIsOutOfRange(int invalidPageSize)
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(pageSize: invalidPageSize);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Page size must be between 1 and 100");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    public void Validate_ShouldFail_WhenMinRatingIsOutOfRange(decimal invalidRating)
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(minRating: invalidRating);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Minimum rating must be between 0 and 5");
    }

    [Fact]
    public void Validate_ShouldPass_WhenQueryIsValid()
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(pageNumber: 1, pageSize: 20, minRating: 3.5m);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }
}
