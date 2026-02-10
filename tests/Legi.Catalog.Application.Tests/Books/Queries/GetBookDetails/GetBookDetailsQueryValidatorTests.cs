using Legi.Catalog.Application.Books.Queries.GetBookDetails;
using Legi.Catalog.Application.Tests.Factories;

namespace Legi.Catalog.Application.Tests.Books.Queries.GetBookDetails;

public class GetBookDetailsQueryValidatorTests
{
    private readonly GetBookDetailsQueryValidator _validator = new();

    [Fact]
    public void Validate_ShouldFail_WhenBookIdIsEmpty()
    {
        // Arrange
        var query = GetBookDetailsQueryFactory.Create(Guid.Empty);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Book ID is required");
    }

    [Fact]
    public void Validate_ShouldPass_WhenQueryIsValid()
    {
        // Arrange
        var query = GetBookDetailsQueryFactory.Create();

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }
}
