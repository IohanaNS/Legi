using Legi.Catalog.Application.Books;
using Legi.Catalog.Application.Books.Commands.CreateBook;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Common.Interfaces;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Commands.CreateBook;

public class CreateBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IWorkRepository> _workRepositoryMock;
    private readonly Mock<IBookDataProvider> _bookDataProviderMock;
    private readonly Mock<IBookCoverUrlResolver> _bookCoverUrlResolverMock;
    private readonly CreateBookCommandHandler _handler;

    public CreateBookCommandHandlerTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _workRepositoryMock = new Mock<IWorkRepository>();
        _bookDataProviderMock = new Mock<IBookDataProvider>();
        _bookCoverUrlResolverMock = new Mock<IBookCoverUrlResolver>();

        _bookDataProviderMock
            .Setup(x => x.GetByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalBookData?)null);

        _bookRepositoryMock
            .Setup(x => x.FindByTitleAndFirstAuthorAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookCoverUrlResolverMock
            .Setup(x => x.ResolveByIsbn(It.IsAny<string>()))
            .Returns((string?)null);

        var bookImportService = new BookImportService(
            _bookRepositoryMock.Object,
            _workRepositoryMock.Object,
            _bookDataProviderMock.Object,
            _bookCoverUrlResolverMock.Object,
            NullLogger<BookImportService>.Instance);

        _handler = new CreateBookCommandHandler(bookImportService);
    }

    [Fact]
    public async Task Handle_ShouldCreateBook_WhenIsbnDoesNotExist()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create();
        Book? persistedBook = null;

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => persistedBook = book)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(command.Isbn, result.Isbn);
        Assert.Equal(command.Title, result.Title);
        Assert.Equal(command.CreatedByUserId, result.CreatedByUserId);
        Assert.NotEmpty(result.Authors);
        Assert.NotEmpty(result.Tags);

        Assert.NotNull(persistedBook);
        Assert.Equal(command.Title, persistedBook!.Title);
        Assert.Equal(command.Isbn, persistedBook.Isbn.Value);

        _bookRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateAndAssignNewWork_WhenNoWorkExistsForKey()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create();
        Book? persistedBook = null;

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);
        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => persistedBook = book)
            .Returns(Task.CompletedTask);
        _workRepositoryMock
            .Setup(x => x.GetByWorkKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Work?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — a new work is created and the book is linked to it.
        Assert.NotNull(persistedBook);
        Assert.NotEqual(Guid.Empty, persistedBook!.WorkId);
        _workRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Work>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReuseExistingWork_WhenWorkExistsForKey()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create();
        var existingWork = Work.Create(WorkKey.Synthesize("Existing", "Author"), "Existing");
        Book? persistedBook = null;

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);
        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => persistedBook = book)
            .Returns(Task.CompletedTask);
        _workRepositoryMock
            .Setup(x => x.GetByWorkKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWork);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — the book links to the existing work; no new work is created.
        Assert.NotNull(persistedBook);
        Assert.Equal(existingWork.Id, persistedBook!.WorkId);
        _workRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Work>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldQueryRepositoryUsingNormalizedIsbn()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(isbn: "978-0-13-235088-4");

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _bookRepositoryMock.Verify(
            x => x.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()),
            Times.Once
        );

        _bookDataProviderMock.Verify(
            x => x.GetByIsbnAsync("9780132350884", It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldPreferUserProvidedFields_WhenExternalDataExists()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTitle("User Title")
            .WithAuthors(["User Author"])
            .WithSynopsis("User synopsis")
            .WithPageCount(321)
            .WithPublisher("User Publisher")
            .WithCoverUrl("https://example.com/user-cover.jpg")
            .WithTags(["user-tag"])
            .Build();

        var externalData = ExternalBookDataBuilder.Valid()
            .WithTitle("External Title")
            .WithAuthors(["External Author"])
            .WithSynopsis("External synopsis")
            .WithPageCount(500)
            .WithPublisher("External Publisher")
            .WithCoverUrl("https://example.com/external-cover.jpg")
            .Build();

        Book? persistedBook = null;

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookDataProviderMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalData);

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Callback<Book, CancellationToken>((book, _) => persistedBook = book)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("User Title", result.Title);
        Assert.Equal("User Author", result.Authors.Single().Name);
        Assert.Equal("User synopsis", result.Synopsis);
        Assert.Equal(321, result.PageCount);
        Assert.Equal("User Publisher", result.Publisher);
        Assert.Equal("https://example.com/user-cover.jpg", result.CoverUrl);
        Assert.Equal("user-tag", result.Tags.Single().Slug);

        Assert.NotNull(persistedBook);
        Assert.Equal("User Title", persistedBook!.Title);
        Assert.Equal("User Author", persistedBook.Authors.Single().Name);
    }

    [Fact]
    public async Task Handle_ShouldUseExternalData_WhenOptionalRequestFieldsAreMissing()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTitle("User Title")
            .WithAuthors(["User Author"])
            .WithoutOptionalMetadata()
            .Build();

        var externalData = ExternalBookDataBuilder.Valid()
            .WithSynopsis("External synopsis")
            .WithPageCount(500)
            .WithPublisher("External Publisher")
            .WithCoverUrl("https://example.com/external-cover.jpg")
            .Build();

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookDataProviderMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalData);

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("User Title", result.Title);
        Assert.Equal("User Author", result.Authors.Single().Name);
        Assert.Equal("External synopsis", result.Synopsis);
        Assert.Equal(500, result.PageCount);
        Assert.Equal("External Publisher", result.Publisher);
        Assert.Equal("https://example.com/external-cover.jpg", result.CoverUrl);
        Assert.Empty(result.Tags);
    }

    [Fact]
    public async Task Handle_ShouldUseExternalTitleAndAuthors_WhenRequestMandatoryFieldsAreMissing()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTitle(" ")
            .WithoutAuthors()
            .WithoutOptionalMetadata()
            .Build();

        var externalData = ExternalBookDataBuilder.Valid()
            .WithTitle("External Title")
            .WithAuthors(["External Author"])
            .Build();

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookDataProviderMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalData);

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("External Title", result.Title);
        Assert.Equal("External Author", result.Authors.Single().Name);
        Assert.Equal("External synopsis.", result.Synopsis);
        Assert.Equal(500, result.PageCount);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenTitleMissingAfterMerge()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTitle(" ")
            .WithAuthors(["User Author"])
            .Build();

        var externalData = ExternalBookDataBuilder.Valid()
            .WithTitle(null)
            .Build();

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookDataProviderMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalData);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<DomainException>(act);
        Assert.Equal("Title is required when not available from external book sources.", exception.Message);

        _bookRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenAuthorsMissingAfterMerge()
    {
        // Arrange
        var command = CreateBookCommandBuilder.Valid()
            .WithTitle("User Title")
            .WithAuthors([])
            .Build();

        var externalData = ExternalBookDataBuilder.Valid()
            .WithAuthors(null)
            .Build();

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookDataProviderMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(externalData);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<DomainException>(act);
        Assert.Equal("At least one author is required when not available from external book sources.", exception.Message);

        _bookRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictException_WhenIsbnAlreadyExists()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create();
        var existingBook = DomainBookFactory.Create();

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBook);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(act);
        Assert.Equal($"A book with ISBN '{command.Isbn}' already exists.", exception.Message);
        Assert.Equal(existingBook.Id, exception.Extensions["existingBookId"]);

        _bookRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _bookDataProviderMock.Verify(
            x => x.GetByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowConflictExceptionWithExistingBookId_WhenTitleAndFirstAuthorAlreadyExist()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(isbn: "9780321125217");
        var existingBook = DomainBookFactory.Create();

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(x => x.FindByTitleAndFirstAuthorAsync(
                command.Title,
                command.Authors![0],
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingBook);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(act);
        Assert.Equal(existingBook.Id, exception.Extensions["existingBookId"]);

        _bookRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _bookDataProviderMock.Verify(
            x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowDomainException_WhenIsbnIsInvalid()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create(isbn: "invalid-isbn");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);

        _bookRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never
        );

        _bookDataProviderMock.Verify(
            x => x.GetByIsbnAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyTags_WhenRequestTagsIsNull()
    {
        // Arrange
        var command = CreateBookCommandFactory.Create() with { Tags = null };

        _bookRepositoryMock
            .Setup(x => x.GetByIsbnAsync(command.Isbn, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        _bookRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(result.Tags);
    }
}
