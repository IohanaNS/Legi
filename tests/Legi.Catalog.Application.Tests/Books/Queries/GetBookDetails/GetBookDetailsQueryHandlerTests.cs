using Legi.Catalog.Application.Books.Queries.GetBookDetails;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Queries.GetBookDetails;

public class GetBookDetailsQueryHandlerTests
{
    private readonly Mock<IBookReadRepository> _bookReadRepositoryMock;
    private readonly GetBookDetailsQueryHandler _handler;

    public GetBookDetailsQueryHandlerTests()
    {
        _bookReadRepositoryMock = new Mock<IBookReadRepository>();
        _handler = new GetBookDetailsQueryHandler(_bookReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnBookDetails_WhenBookExists()
    {
        // Arrange
        var query = GetBookDetailsQueryFactory.Create();
        var details = BookReadResultFactory.CreateDetails(id: query.BookId);

        _bookReadRepositoryMock
            .Setup(x => x.GetBookDetailsByIdAsync(query.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(details.Id, result.Id);
        Assert.Equal(details.Isbn, result.Isbn);
        Assert.Equal(details.Title, result.Title);
        Assert.Equal(details.Synopsis, result.Synopsis);
        Assert.Equal(details.PageCount, result.PageCount);
        Assert.Equal(details.Publisher, result.Publisher);
        Assert.Equal(details.CoverUrl, result.CoverUrl);
        Assert.Equal(details.AverageRating, result.AverageRating);
        Assert.Equal(details.RatingsCount, result.RatingsCount);
        Assert.Equal(details.ReviewsCount, result.ReviewsCount);
        Assert.Equal(details.CreatedByUserId, result.CreatedByUserId);
        Assert.Equal(details.CreatedAt, result.CreatedAt);
        Assert.Equal(details.UpdatedAt, result.UpdatedAt);
        Assert.Equal(details.Authors.Count, result.Authors.Count);
        Assert.Equal(details.Tags.Count, result.Tags.Count);

        _bookReadRepositoryMock.Verify(
            x => x.GetBookDetailsByIdAsync(query.BookId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenBookDoesNotExist()
    {
        // Arrange
        var query = GetBookDetailsQueryFactory.Create();

        _bookReadRepositoryMock
            .Setup(x => x.GetBookDetailsByIdAsync(query.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookDetailsResult?)null);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(act);
        Assert.Equal($"Book with key '{query.BookId}' was not found.", exception.Message);
    }
}
