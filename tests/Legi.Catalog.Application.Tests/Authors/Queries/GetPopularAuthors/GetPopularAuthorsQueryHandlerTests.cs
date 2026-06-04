using Legi.Catalog.Application.Authors.Queries.GetPopularAuthors;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Authors.Queries.GetPopularAuthors;

public class GetPopularAuthorsQueryHandlerTests
{
    private readonly Mock<IAuthorReadRepository> _authorReadRepositoryMock = new();
    private readonly GetPopularAuthorsQueryHandler _handler;

    public GetPopularAuthorsQueryHandlerTests()
    {
        _handler = new GetPopularAuthorsQueryHandler(_authorReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedAuthors_WhenResultsExist()
    {
        // Arrange
        var query = new GetPopularAuthorsQuery(Limit: 3);
        var results = new List<AuthorSearchResult>
        {
            new("Agatha Christie", "agatha-christie", 12),
            new("Stephen King", "stephen-king", 9)
        };

        _authorReadRepositoryMock
            .Setup(x => x.GetPopularAsync(query.Limit, It.IsAny<CancellationToken>()))
            .ReturnsAsync(results);

        // Act
        var response = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, response.Authors.Count);
        Assert.Equal("Agatha Christie", response.Authors[0].Name);
        Assert.Equal("agatha-christie", response.Authors[0].Slug);
        Assert.Equal(12, response.Authors[0].BooksCount);

        _authorReadRepositoryMock.Verify(
            x => x.GetPopularAsync(query.Limit, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
