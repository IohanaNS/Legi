using Legi.Catalog.Application.Authors.Queries.SearchAuthors;

namespace Legi.Catalog.Application.Tests.Authors.Queries.SearchAuthors;

public class SearchAuthorsQueryValidatorTests
{
    private readonly SearchAuthorsQueryValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenSearchTermIsEmpty(string invalidSearchTerm)
    {
        // Arrange
        var query = new SearchAuthorsQuery(invalidSearchTerm);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Search term is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenSearchTermIsTooLong()
    {
        // Arrange
        var query = new SearchAuthorsQuery(new string('a', 101));

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Search term must be at most 100 characters");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_ShouldFail_WhenLimitIsOutOfRange(int invalidLimit)
    {
        // Arrange
        var query = new SearchAuthorsQuery("martin", invalidLimit);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Limit must be between 1 and 50");
    }

    [Fact]
    public void Validate_ShouldPass_WhenQueryIsValid()
    {
        // Arrange
        var query = new SearchAuthorsQuery("martin", Limit: 10);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }
}
