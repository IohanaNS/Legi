using Legi.Catalog.Application.Books.Queries.SearchBooks;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Queries.SearchBooks;

public class SearchBooksQueryHandlerTests
{
    private readonly Mock<IBookReadRepository> _bookReadRepositoryMock;
    private readonly SearchBooksQueryHandler _handler;

    public SearchBooksQueryHandlerTests()
    {
        _bookReadRepositoryMock = new Mock<IBookReadRepository>();
        _handler = new SearchBooksQueryHandler(_bookReadRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedBooksAndPagination_WhenResultsExist()
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(pageNumber: 2, pageSize: 10);

        var books = new List<BookSearchResult>
        {
            BookReadResultFactory.CreateSearchResult(),
            BookReadResultFactory.CreateSearchResult(
                id: Guid.Parse("33333333-3333-3333-3333-333333333333"),
                isbn: "9780321125217",
                title: "Domain-Driven Design",
                authors: [("Eric Evans", "eric-evans")],
                tags: [("ddd", "ddd")])
        };

        const int totalCount = 25;

        _bookReadRepositoryMock
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                query.AuthorSlug,
                query.TagSlug,
                query.MinRating,
                query.PageNumber,
                query.PageSize,
                query.SortBy,
                query.SortDescending,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((books, totalCount));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Books.Count);
        var bookTitles = result.Books.Select(b => b.Title).ToList();
        Assert.Contains("Clean Code", bookTitles);
        Assert.Contains("Domain-Driven Design", bookTitles);

        Assert.Equal(2, result.Pagination.CurrentPage);
        Assert.Equal(10, result.Pagination.PageSize);
        Assert.Equal(totalCount, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.True(result.Pagination.HasPrevious);
        Assert.True(result.Pagination.HasNext);

        _bookReadRepositoryMock.Verify(
            x => x.SearchAsync(
                query.SearchTerm,
                query.AuthorSlug,
                query.TagSlug,
                query.MinRating,
                query.PageNumber,
                query.PageSize,
                query.SortBy,
                query.SortDescending,
                It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPagination_WhenNoResultsExist()
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(pageNumber: 1, pageSize: 20);

        _bookReadRepositoryMock
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                query.AuthorSlug,
                query.TagSlug,
                query.MinRating,
                query.PageNumber,
                query.PageSize,
                query.SortBy,
                query.SortDescending,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<BookSearchResult>(), 0));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result.Books);
        Assert.Equal(1, result.Pagination.CurrentPage);
        Assert.Equal(20, result.Pagination.PageSize);
        Assert.Equal(0, result.Pagination.TotalCount);
        Assert.Equal(0, result.Pagination.TotalPages);
        Assert.False(result.Pagination.HasPrevious);
        Assert.False(result.Pagination.HasNext);
    }
}
