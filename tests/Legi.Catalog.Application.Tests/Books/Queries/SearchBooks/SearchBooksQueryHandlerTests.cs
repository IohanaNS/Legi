using Legi.Catalog.Application.Books.Queries.SearchBooks;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Queries.SearchBooks;

public class SearchBooksQueryHandlerTests
{
    private readonly Mock<IBookReadRepository> _bookReadRepositoryMock;
    private readonly Mock<IExternalBookSearchQueue> _externalBookSearchQueueMock;
    private readonly SearchBooksQueryHandler _handler;

    public SearchBooksQueryHandlerTests()
    {
        _bookReadRepositoryMock = new Mock<IBookReadRepository>();
        _externalBookSearchQueueMock = new Mock<IExternalBookSearchQueue>();
        _externalBookSearchQueueMock
            .Setup(q => q.QueueAsync(
                It.IsAny<ExternalBookSearchQueueRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExternalBookSearchEnrichment(
                ExternalBookSearchEnrichmentStatuses.Queued,
                "External search was queued.",
                5));
        _handler = new SearchBooksQueryHandler(
            _bookReadRepositoryMock.Object,
            _externalBookSearchQueueMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnMappedBooksAndPagination_WhenResultsExist()
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(pageNumber: 2, pageSize: 10);

        var books = new List<BookSearchResult>
        {
            BookSearchResultBuilder.Valid()
                .WithAuthors([("Robert C. Martin", "robert-c-martin")])
                .WithTags([("software-engineering", "software-engineering")])
                .Build(),
            BookSearchResultBuilder.Valid()
                .WithId(Guid.Parse("33333333-3333-3333-3333-333333333333"))
                .WithIsbn("9780321125217")
                .WithTitle("Domain-Driven Design")
                .WithAuthors([("Eric Evans", "eric-evans")])
                .WithTags([("ddd", "ddd")])
                .WithCoverUrl(null)
                .Build()
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

        var domainDrivenDesign = result.Books.Single(b => b.Title == "Domain-Driven Design");
        Assert.Equal("9780321125217", domainDrivenDesign.Isbn);
        Assert.Equal("Eric Evans", domainDrivenDesign.Authors.Single().Name);
        Assert.Equal("eric-evans", domainDrivenDesign.Authors.Single().Slug);
        Assert.Equal("ddd", domainDrivenDesign.Tags.Single().Name);
        Assert.Equal("ddd", domainDrivenDesign.Tags.Single().Slug);
        Assert.Null(domainDrivenDesign.CoverUrl);

        Assert.Equal(2, result.Pagination.CurrentPage);
        Assert.Equal(10, result.Pagination.PageSize);
        Assert.Equal(totalCount, result.Pagination.TotalCount);
        Assert.Equal(3, result.Pagination.TotalPages);
        Assert.True(result.Pagination.HasPrevious);
        Assert.True(result.Pagination.HasNext);
        Assert.Equal(ExternalBookSearchEnrichmentStatuses.NotApplicable, result.Enrichment.Status);

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
        var query = SearchBooksQueryFactory.Create(pageNumber: 1, pageSize: 20, minRating: null);

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
        Assert.Equal(ExternalBookSearchEnrichmentStatuses.Queued, result.Enrichment.Status);
        Assert.Equal(5, result.Enrichment.RefreshAfterSeconds);

        _externalBookSearchQueueMock.Verify(
            q => q.QueueAsync(
                It.Is<ExternalBookSearchQueueRequest>(r =>
                    r.SearchTerm == query.SearchTerm &&
                    r.RequestedByUserId == query.AuthenticatedUserId &&
                    r.MaxResults == 50),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMarkEnrichmentNotNeeded_WhenPlainSearchHasFullLocalPage()
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(pageNumber: 1, pageSize: 2, minRating: null);
        var books = new List<BookSearchResult>
        {
            BookSearchResultBuilder.Valid().Build(),
            BookSearchResultBuilder.Valid()
                .WithId(Guid.Parse("33333333-3333-3333-3333-333333333333"))
                .WithIsbn("9780321125217")
                .WithTitle("Domain-Driven Design")
                .Build()
        };

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
            .ReturnsAsync((books, 2));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ExternalBookSearchEnrichmentStatuses.NotNeeded, result.Enrichment.Status);
        _externalBookSearchQueueMock.Verify(
            q => q.QueueAsync(
                It.IsAny<ExternalBookSearchQueueRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldNotQueueEnrichment_WhenSearchHasAdditionalFilters()
    {
        // Arrange
        var query = SearchBooksQueryFactory.Create(tagSlug: "fantasy", minRating: null);

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
        Assert.Equal(ExternalBookSearchEnrichmentStatuses.NotApplicable, result.Enrichment.Status);
        _externalBookSearchQueueMock.Verify(
            q => q.QueueAsync(
                It.IsAny<ExternalBookSearchQueueRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
