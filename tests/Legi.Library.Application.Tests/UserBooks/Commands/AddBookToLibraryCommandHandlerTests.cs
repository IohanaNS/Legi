using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.AddBookToLibrary;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Enums;
using Legi.Library.Domain.Repositories;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.Commands;

public class AddBookToLibraryCommandHandlerTests
{
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly AddBookToLibraryCommandHandler _handler;

    public AddBookToLibraryCommandHandlerTests()
    {
        _handler = new AddBookToLibraryCommandHandler(
            _userBookRepository.Object,
            _bookSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_BookExistsAndNotAlreadyInLibrary_AddsUserBook()
    {
        var command = AddBookToLibraryCommandBuilder.Valid()
            .AsWishlist()
            .Build();
        var snapshot = BookSnapshotFactory.Create(bookId: command.BookId);
        UserBook? addedUserBook = null;

        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(command.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        _userBookRepository
            .Setup(r => r.GetByUserAndBookAsync(command.UserId, command.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserBook?)null);
        _userBookRepository
            .Setup(r => r.AddAsync(It.IsAny<UserBook>(), It.IsAny<CancellationToken>()))
            .Callback<UserBook, CancellationToken>((userBook, _) => addedUserBook = userBook)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(addedUserBook);
        Assert.Equal(command.UserId, addedUserBook.UserId);
        Assert.Equal(command.BookId, addedUserBook.BookId);
        Assert.True(addedUserBook.WishList);
        Assert.Equal(ReadingStatus.NotStarted, addedUserBook.Status);

        Assert.Equal(addedUserBook.Id, response.UserBookId);
        Assert.Equal(command.BookId, response.BookId);
        Assert.Equal("NotStarted", response.Status);
        Assert.True(response.Wishlist);
    }

    [Fact]
    public async Task Handle_BookSnapshotMissing_ThrowsNotFoundException()
    {
        var command = AddBookToLibraryCommandBuilder.Valid().Build();
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(command.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookSnapshot?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userBookRepository.Verify(
            r => r.AddAsync(It.IsAny<UserBook>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserAlreadyHasBook_ThrowsConflictException()
    {
        var existing = UserBookBuilder.Valid().Build();
        var command = AddBookToLibraryCommandBuilder.Valid()
            .WithUserId(existing.UserId)
            .WithBookId(existing.BookId)
            .Build();

        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(command.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshotFactory.Create(bookId: command.BookId));
        _userBookRepository
            .Setup(r => r.GetByUserAndBookAsync(command.UserId, command.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userBookRepository.Verify(
            r => r.AddAsync(It.IsAny<UserBook>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
