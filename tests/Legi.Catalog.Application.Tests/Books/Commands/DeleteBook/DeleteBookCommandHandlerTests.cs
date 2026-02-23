using Legi.Catalog.Application.Books.Commands.DeleteBook;
using Legi.Catalog.Application.Common.Exceptions;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.Commands.DeleteBook;

public class DeleteBookCommandHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly DeleteBookCommandHandler _handler;

    public DeleteBookCommandHandlerTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _handler = new DeleteBookCommandHandler(_bookRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldDeleteBook_WhenBookExists()
    {
        // Arrange
        var book = DomainBookFactory.Create();
        var command = new DeleteBookCommand(book.Id);

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        _bookRepositoryMock
            .Setup(x => x.DeleteAsync(book, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _bookRepositoryMock.Verify(
            x => x.GetByIdAsync(book.Id, It.IsAny<CancellationToken>()),
            Times.Once);

        _bookRepositoryMock.Verify(
            x => x.DeleteAsync(book, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenBookDoesNotExist()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var command = new DeleteBookCommand(bookId);

        _bookRepositoryMock
            .Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);

        _bookRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
