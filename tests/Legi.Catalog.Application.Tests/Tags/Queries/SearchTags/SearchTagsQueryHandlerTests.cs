using Legi.Catalog.Application.Tags.Queries.SearchTags;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Tags.Queries.SearchTags;

public class SearchTagsQueryHandlerTests
{
    private readonly Mock<ITagReadRepository> _tagReadRepositoryMock = new();
    private readonly SearchTagsQueryHandler _handler;

    public SearchTagsQueryHandlerTests()
    {
        _handler = new SearchTagsQueryHandler(_tagReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedTags_WhenResultsExist()
    {
        // Arrange
        var query = new SearchTagsQuery("arch", Limit: 5);
        var results = new List<TagSearchResult>
        {
            new("architecture", "architecture", 10),
            new("software-architecture", "software-architecture", 4)
        };

        _tagReadRepositoryMock
            .Setup(x => x.SearchAsync(query.SearchTerm, query.Limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, response.Tags.Count);
        Assert.Equal("architecture", response.Tags[0].Name);
        Assert.Equal("architecture", response.Tags[0].Slug);
        Assert.Equal(10, response.Tags[0].UsageCount);
        Assert.Equal("software-architecture", response.Tags[1].Name);

        _tagReadRepositoryMock.Verify(
            x => x.SearchAsync(query.SearchTerm, query.Limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyTags_WhenRepositoryReturnsNoResults()
    {
        // Arrange
        var query = new SearchTagsQuery("missing", Limit: 10);

        _tagReadRepositoryMock
            .Setup(x => x.SearchAsync(query.SearchTerm, query.Limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(response.Tags);
    }
}
