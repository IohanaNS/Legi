using Legi.Social.Application.Profiles.Queries.SearchUsers;

namespace Legi.Social.Application.Tests.Profiles.Queries.SearchUsers;

public class SearchUsersQueryValidatorTests
{
    private readonly SearchUsersQueryValidator _validator = new();

    [Fact]
    public void Validate_UsernamePrefixShorterThanMinimum_Fails()
    {
        var query = new SearchUsersQuery("ab", null);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchUsersQuery.UsernamePrefix));
    }

    [Theory]
    [InlineData("ali-")]
    [InlineData("ali.ce")]
    public void Validate_UsernamePrefixWithInvalidFormat_Fails(string usernamePrefix)
    {
        var query = new SearchUsersQuery(usernamePrefix, null);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchUsersQuery.UsernamePrefix));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void Validate_LimitOutsideAllowedRange_Fails(int limit)
    {
        var query = new SearchUsersQuery("alice", null, limit);

        var result = _validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(SearchUsersQuery.Limit));
    }

    [Fact]
    public void Validate_MixedCaseValidPrefix_Passes()
    {
        var query = new SearchUsersQuery(" 1Ali_ ", Guid.NewGuid(), 20);

        var result = _validator.Validate(query);

        Assert.True(result.IsValid);
    }
}
