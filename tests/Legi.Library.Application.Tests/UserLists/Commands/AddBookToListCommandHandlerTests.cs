using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserLists.Commands.AddBookToList;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Moq;

namespace Legi.Library.Application.Tests.UserLists.Commands;

public class AddBookToListCommandHandlerTests
{
    private readonly Mock<IUserListRepository> _userListRepository = new();
    private readonly Mock<IUserBookRepository> _userBookRepository = new();
    private readonly AddBookToListCommandHandler _handler;

    public AddBookToListCommandHandlerTests()
    {
        _handler = new AddBookToListCommandHandler(
            _userListRepository.Object,
            _userBookRepository.Object);
    }

    [Fact]
    public async Task Handle_UserOwnsListAndBook_AddsBookToList()
    {
        var list = UserListBuilder.Valid().Build();
        var userBook = UserBookBuilder.Valid().Build();
        var command = AddBookToListCommandBuilder.Valid()
            .WithListId(list.Id)
            .WithUserBookId(userBook.Id)
            .Build();

        _userListRepository
            .Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);
        _userListRepository
            .Setup(r => r.UpdateAsync(list, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(list.Id, response.ListId);
        Assert.Equal(userBook.Id, response.UserBookId);
        Assert.Equal(1, response.BooksCount);
        Assert.Single(list.Items, item => item.UserBookId == userBook.Id);
        _userListRepository.Verify(r => r.UpdateAsync(list, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnList_ThrowsForbiddenException()
    {
        var list = UserListBuilder.Valid().Build();
        var command = AddBookToListCommandBuilder.Valid()
            .WithListId(list.Id)
            .WithUserId(LibraryTestIds.OtherUserId)
            .Build();

        _userListRepository
            .Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userBookRepository.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userListRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserDoesNotOwnBook_ThrowsForbiddenException()
    {
        var list = UserListBuilder.Valid().Build();
        var userBook = UserBookBuilder.Valid()
            .WithUserId(LibraryTestIds.OtherUserId)
            .Build();
        var command = AddBookToListCommandBuilder.Valid()
            .WithListId(list.Id)
            .WithUserBookId(userBook.Id)
            .Build();

        _userListRepository
            .Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _userBookRepository
            .Setup(r => r.GetByIdAsync(userBook.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userBook);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userListRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
