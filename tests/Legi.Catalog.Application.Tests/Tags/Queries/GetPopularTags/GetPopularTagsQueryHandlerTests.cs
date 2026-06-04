using Legi.Catalog.Application.Tags.Queries.GetPopularTags;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Tags.Queries.GetPopularTags;

public class GetPopularTagsQueryHandlerTests
{
    private readonly Mock<ITagReadRepository> _tagReadRepositoryMock = new();
    private readonly GetPopularTagsQueryHandler _handler;

    public GetPopularTagsQueryHandlerTests()
    {
        _handler = new GetPopularTagsQueryHandler(_tagReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedTags_WhenResultsExist()
    {
        // Arrange
        var query = new GetPopularTagsQuery(Limit: 3);
        var results = new List<TagSearchResult>
        {
            new("fiction", "fiction", 18),
            new("non-fiction", "non-fiction", 11)
        };

        _tagReadRepositoryMock
            .Setup(x => x.GetPopularAsync(query.Limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, response.Tags.Count);
        Assert.Equal("fiction", response.Tags[0].Name);
        Assert.Equal("fiction", response.Tags[0].Slug);
        Assert.Equal(18, response.Tags[0].UsageCount);

        _tagReadRepositoryMock.Verify(
            x => x.GetPopularAsync(query.Limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
