using Legi.Catalog.Application.Tags.Queries.GetPopularTags;

namespace Legi.Catalog.Application.Tests.Tags.Queries.GetPopularTags;

public class GetPopularTagsQueryValidatorTests
{
    private readonly GetPopularTagsQueryValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_ShouldFail_WhenLimitIsOutOfRange(int invalidLimit)
    {
        // Arrange
        var query = new GetPopularTagsQuery(invalidLimit);

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
        var query = new GetPopularTagsQuery(Limit: 20);

        // Act
        var result = _validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }
}
