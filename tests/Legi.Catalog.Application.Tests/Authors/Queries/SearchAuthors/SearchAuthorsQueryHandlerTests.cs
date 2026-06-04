using Legi.Catalog.Application.Authors.Queries.SearchAuthors;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Authors.Queries.SearchAuthors;

public class SearchAuthorsQueryHandlerTests
{
    private readonly Mock<IAuthorReadRepository> _authorReadRepositoryMock = new();
    private readonly SearchAuthorsQueryHandler _handler;

    public SearchAuthorsQueryHandlerTests()
    {
        _handler = new SearchAuthorsQueryHandler(_authorReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedAuthors_WhenResultsExist()
    {
        // Arrange
        var query = new SearchAuthorsQuery("mart", Limit: 5);
        var results = new List<AuthorSearchResult>
        {
            new("Robert C. Martin", "robert-c-martin", 3),
            new("Martin Fowler", "martin-fowler", 2)
        };

        _authorReadRepositoryMock
            .Setup(x => x.SearchAsync(query.SearchTerm, query.Limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, response.Authors.Count);
        Assert.Equal("Robert C. Martin", response.Authors[0].Name);
        Assert.Equal("robert-c-martin", response.Authors[0].Slug);
        Assert.Equal(3, response.Authors[0].BooksCount);
        Assert.Equal("Martin Fowler", response.Authors[1].Name);

        _authorReadRepositoryMock.Verify(
            x => x.SearchAsync(query.SearchTerm, query.Limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyAuthors_WhenRepositoryReturnsNoResults()
    {
        // Arrange
        var query = new SearchAuthorsQuery("missing", Limit: 10);

        _authorReadRepositoryMock
            .Setup(x => x.SearchAsync(query.SearchTerm, query.Limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(response.Authors);
    }
}
