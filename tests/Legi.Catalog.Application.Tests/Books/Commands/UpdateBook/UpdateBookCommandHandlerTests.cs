using Legi.Catalog.Application.Books.Commands.UpdateBook;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Commands.UpdateBook;

public class UpdateBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly UpdateBookCommandHandler _handler;

    public UpdateBookCommandHandlerTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _handler = new UpdateBookCommandHandler(_bookRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateTitle_WhenOnlyTitleProvided()
    {
        // Arrange
        var book = DomainBookFactory.Create();
        var command = new UpdateBookCommand(book.Id, Title: "New Title");

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("New Title", result.Title);
        Assert.Equal(book.Id, result.BookId);

        _bookRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAuthors_WhenAuthorsProvided()
    {
        // Arrange
        var book = DomainBookFactory.Create();
        var command = new UpdateBookCommand(book.Id, Authors: ["New Author One", "New Author Two"]);

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Authors.Count);
        Assert.Contains(result.Authors, a => a.Name == "New Author One");
        Assert.Contains(result.Authors, a => a.Name == "New Author Two");
    }

    [Fact]
    public async Task Handle_ShouldReplaceTags_WhenTagsProvided()
    {
        // Arrange
        var book = DomainBookFactory.Create();
        var command = new UpdateBookCommand(book.Id, Tags: ["new-tag-1", "new-tag-2"]);

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Tags.Count);
        Assert.Contains(result.Tags, t => t.Slug == "new-tag-1");
        Assert.Contains(result.Tags, t => t.Slug == "new-tag-2");
    }

    [Fact]
    public async Task Handle_ShouldClearTags_WhenEmptyTagsProvided()
    {
        // Arrange
        var book = DomainBookFactory.Create();
        var command = new UpdateBookCommand(book.Id, Tags: []);

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Empty(result.Tags);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenBookDoesNotExist()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var command = new UpdateBookCommand(bookId, Title: "New Title");

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);

        _bookRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateMultipleFields_WhenMultipleFieldsProvided()
    {
        // Arrange
        var book = DomainBookFactory.Create();
        var command = new UpdateBookCommand(
            book.Id,
            Title: "Updated Title",
            Synopsis: "Updated synopsis",
            PageCount: 300,
            Publisher: "New Publisher",
            CoverUrl: "https://example.com/new-cover.jpg");

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _bookRepositoryMock
            .Setup(x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Updated Title", result.Title);
        Assert.Equal("Updated synopsis", result.Synopsis);
        Assert.Equal(300, result.PageCount);
        Assert.Equal("New Publisher", result.Publisher);
        Assert.Equal("https://example.com/new-cover.jpg", result.CoverUrl);
    }
}
