using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.Commands.RemoveBookFromLibrary;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.Commands;

public class RemoveBookFromLibraryCommandHandlerTests
{
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly Mock<IUserListRepository> _userListRepository = new();
    private readonly RemoveBookFromLibraryCommandHandler _handler;

    public RemoveBookFromLibraryCommandHandlerTests()
    {
        _handler = new RemoveBookFromLibraryCommandHandler(
            _userBookRepository.Object,
            _userListRepository.Object);
    }

    [Fact]
    public async Task Handle_UserOwnsBook_SoftDeletesBookAndRemovesListReferences()
    {
        var userBook = UserBookBuilder.Valid().Build();
        var firstList = UserListBuilder.Valid()
            .WithBook(userBook.Id)
            .Build();
        var secondList = UserListBuilder.Valid()
            .WithBook(userBook.Id)
            .Build();
        var command = RemoveBookFromLibraryCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userListRepository
            .Setup(r => r.GetListsContainingBookAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstList, secondList]);
        _userListRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _userBookRepository
            .Setup(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Handle(command, CancellationToken.None);

        Assert.True(userBook.IsDeleted);
        Assert.Equal(0, firstList.BooksCount);
        Assert.Equal(0, secondList.BooksCount);
        _userListRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        _userBookRepository.Verify(r => r.UpdateAsync(userBook, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnBook_ThrowsForbiddenException()
    {
        var userBook = UserBookBuilder.Valid().Build();
        var command = RemoveBookFromLibraryCommandBuilder.Valid()
            .WithUserBookId(userBook.Id)
            .WithUserId(LibraryTestIds.OtherUserId)
            .Build();

        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userListRepository.Verify(
            r => r.GetListsContainingBookAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userBookRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserBook>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
