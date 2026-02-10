using Legi.Catalog.Application.Books.Commands.CreateBook;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.SharedKernel;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Commands.CreateBook;

public class CreateBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly CreateBookCommandHandler _handler;

    public CreateBookCommandHandlerTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _handler = new CreateBookCommandHandler(_bookRepositoryMock.Object);
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

        _bookRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never
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
