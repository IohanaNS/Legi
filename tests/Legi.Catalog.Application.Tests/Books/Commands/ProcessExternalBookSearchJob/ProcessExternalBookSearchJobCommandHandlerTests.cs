using Legi.Catalog.Application.Books;
using Legi.Catalog.Application.Books.Commands.ProcessExternalBookSearchJob;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Application.Common.Storage;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Commands.ProcessExternalBookSearchJob;

public class ProcessExternalBookSearchJobCommandHandlerTests
{
    private const string IsbnCoverUrl = "https://covers.openlibrary.org/b/isbn/fallback-L.jpg";

    private readonly Mock<IBookDataProvider> _bookDataProviderMock = new();
    private readonly Mock<IBookRepository> _bookRepositoryMock = new();
    private readonly Mock<IWorkRepository> _workRepositoryMock = new();
    private readonly Mock<IBookSearchAliasWriter> _searchAliasWriterMock = new();
    private readonly Mock<IBookCoverUrlResolver> _coverUrlResolverMock = new();
    private readonly Mock<IBookCoverAcquisition> _coverAcquisitionMock = new();
    private readonly Mock<ICoverIngestionQueue> _coverIngestionQueueMock = new();
    private readonly ProcessExternalBookSearchJobCommandHandler _handler;

    public ProcessExternalBookSearchJobCommandHandlerTests()
    {
        _coverUrlResolverMock
            .Setup(r => r.ResolveByIsbn(It.IsAny<string>()))
            .Returns(IsbnCoverUrl);

        // Stand-in for acquire-cover: validate+store the first usable candidate and
        // return its (owned) URL. Echoing the first non-blank candidate lets these
        // tests assert which cover flowed through (candidate vs. ISBN fallback).
        _coverAcquisitionMock
            .Setup(x => x.AcquireAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string?>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, IReadOnlyList<string?> candidates, CancellationToken _) =>
                candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c)));

        var bookImportService = new BookImportService(
            _bookRepositoryMock.Object,
            _workRepositoryMock.Object,
            _bookDataProviderMock.Object,
            _coverUrlResolverMock.Object,
            _coverAcquisitionMock.Object,
            _coverIngestionQueueMock.Object,
            NullLogger<BookImportService>.Instance);

        _handler = new ProcessExternalBookSearchJobCommandHandler(
            _bookDataProviderMock.Object,
            bookImportService,
            _searchAliasWriterMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldImportMissingBook_WhenCandidateIsValid()
    {
        // Arrange
        var command = CreateCommand();
        Book? persistedBook = null;
        var candidate = CreateCandidate();

        _bookDataProviderMock
            .Setup(p => p.SearchAsync(command.SearchTerm, command.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);

        _bookRepositoryMock
            .Setup(r => r.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.FindByTitleAndFirstAuthorAsync("Clean Code", "Robert C. Martin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => persistedBook = book)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.CandidatesFound);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(0, result.SkippedCount);

        Assert.NotNull(persistedBook);
        Assert.Equal("9780132350884", persistedBook!.Isbn.Value);
        Assert.Equal("Clean Code", persistedBook.Title);
        Assert.Equal("Robert C. Martin", persistedBook.Authors.Single().Name);
        Assert.Equal("https://example.com/clean-code.jpg", persistedBook.CoverUrl);
        Assert.Equal("software-engineering", persistedBook.Tags.Single().Slug);
    }

    [Fact]
    public async Task Handle_ShouldFillMissingExistingFields_WithoutOverwritingExistingMetadata()
    {
        // Arrange
        var command = CreateCommand();
        var existingBook = DomainBookFactory.Create();
        var candidate = CreateCandidate(
            title: "External Title Should Not Win",
            authors: ["External Author"],
            synopsis: "External synopsis",
            pageCount: 464,
            publisher: "Prentice Hall",
            coverUrl: "https://example.com/clean-code.jpg",
            tags: ["software-engineering"]);

        _bookDataProviderMock
            .Setup(p => p.SearchAsync(command.SearchTerm, command.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);

        _bookRepositoryMock
            .Setup(r => r.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBook);

        _bookRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.UpdatedCount);
        Assert.Equal(0, result.SkippedCount);

        Assert.Equal("Clean Code", existingBook.Title);
        Assert.Equal("Robert C. Martin", existingBook.Authors.Single().Name);
        Assert.Equal("External synopsis", existingBook.Synopsis);
        Assert.Equal(464, existingBook.PageCount);
        Assert.Equal("Prentice Hall", existingBook.Publisher);
        Assert.Equal("https://example.com/clean-code.jpg", existingBook.CoverUrl);
        Assert.Equal("software-engineering", existingBook.Tags.Single().Slug);

        _bookRepositoryMock.Verify(
            r => r.UpdateAsync(existingBook, It.IsAny<CancellationToken>()),
            Times.Once);
        _bookRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDeduplicateCandidates_ByIsbn()
    {
        // Arrange
        var command = CreateCommand();

        _bookDataProviderMock
            .Setup(p => p.SearchAsync(command.SearchTerm, command.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateCandidate(provider: "OpenLibrary"),
                CreateCandidate(provider: "GoogleBooks")
            ]);

        _bookRepositoryMock
            .Setup(r => r.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.FindByTitleAndFirstAuthorAsync("Clean Code", "Robert C. Martin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.CandidatesFound);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);

        _bookRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSkipCandidate_WhenMandatoryCatalogFieldsAreMissing()
    {
        // Arrange
        var command = CreateCommand();

        _bookDataProviderMock
            .Setup(p => p.SearchAsync(command.SearchTerm, command.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                CreateCandidate(isbn13: null),
                CreateCandidate(isbn13: "9780321125217", title: null),
                CreateCandidate(isbn13: "9780201616224", authors: [])
            ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.CandidatesFound);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(3, result.SkippedCount);

        _bookRepositoryMock.Verify(
            r => r.GetByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _bookRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAliasImportedBook_ToTheSearchQuery()
    {
        // Arrange
        var command = CreateCommand();
        Book? persistedBook = null;
        var candidate = CreateCandidate();

        _bookDataProviderMock
            .Setup(p => p.SearchAsync(command.SearchTerm, command.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);

        _bookRepositoryMock
            .Setup(r => r.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.FindByTitleAndFirstAuthorAsync("Clean Code", "Robert C. Martin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => persistedBook = book)
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(persistedBook);
        _searchAliasWriterMock.Verify(
            w => w.LinkAsync(
                command.SearchTerm,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(persistedBook!.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAliasExistingBook_EvenWhenImportIsSkipped()
    {
        // Arrange — an existing complete book that the candidate adds nothing to:
        // the import is skipped, but the query still resolved to it, so it must be
        // aliased. The book already has a cover, so the ISBN fallback doesn't apply.
        var command = CreateCommand();
        var existingBook = DomainBookFactory.Create();
        existingBook.UpdateDetails(coverUrl: "https://example.com/existing-cover.jpg");
        var barrenCandidate = CreateCandidate(
            synopsis: null,
            pageCount: null,
            publisher: null,
            coverUrl: null,
            tags: []);

        _bookDataProviderMock
            .Setup(p => p.SearchAsync(command.SearchTerm, command.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([barrenCandidate]);

        _bookRepositoryMock
            .Setup(r => r.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBook);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(0, result.UpdatedCount);

        _searchAliasWriterMock.Verify(
            w => w.LinkAsync(
                command.SearchTerm,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(existingBook.Id)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFallBackToIsbnCover_WhenCandidateHasNoCover()
    {
        // Arrange
        var command = CreateCommand();
        Book? persistedBook = null;
        var candidate = CreateCandidate(coverUrl: null);

        _bookDataProviderMock
            .Setup(p => p.SearchAsync(command.SearchTerm, command.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);

        _bookRepositoryMock
            .Setup(r => r.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.FindByTitleAndFirstAuthorAsync("Clean Code", "Robert C. Martin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => persistedBook = book)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ImportedCount);
        Assert.NotNull(persistedBook);
        Assert.Equal(IsbnCoverUrl, persistedBook!.CoverUrl);
        _coverUrlResolverMock.Verify(r => r.ResolveByIsbn("9780132350884"), Times.Once);
    }

    private static ProcessExternalBookSearchJobCommand CreateCommand()
    {
        return new ProcessExternalBookSearchJobCommand(
            "clean code",
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            25);
    }

    private static ExternalBookCandidate CreateCandidate(
        string provider = "OpenLibrary",
        string? isbn10 = null,
        string? isbn13 = "9780132350884",
        string? title = "Clean Code",
        IReadOnlyList<string>? authors = null,
        string? synopsis = "External synopsis",
        int? pageCount = 464,
        string? publisher = "Prentice Hall",
        string? coverUrl = "https://example.com/clean-code.jpg",
        IReadOnlyList<string>? tags = null)
    {
        return new ExternalBookCandidate
        {
            Provider = provider,
            ProviderBookId = "provider-book-id",
            Isbn10 = isbn10,
            Isbn13 = isbn13,
            Title = title,
            Authors = authors ?? ["Robert C. Martin"],
            Synopsis = synopsis,
            PageCount = pageCount,
            Publisher = publisher,
            CoverUrl = coverUrl,
            Tags = tags ?? ["software-engineering"],
            Language = "en",
            PublishedDate = "2008"
        };
    }
}
