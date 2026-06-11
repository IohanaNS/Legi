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
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly AddBookToListCommandHandler _handler;

    public AddBookToListCommandHandlerTests()
    {
        _handler = new AddBookToListCommandHandler(
            _userListRepository.Object,
            _bookSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_UserOwnsListAndBookHasSnapshot_AddsBookToList()
    {
        var list = UserListBuilder.Valid().Build();
        var command = AddBookToListCommandBuilder.Valid()
            .WithListId(list.Id)
            .WithBookId(LibraryTestIds.BookId)
            .Build();

        _userListRepository
            .Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(LibraryTestIds.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshotFactory.Create(LibraryTestIds.BookId));
        _userListRepository
            .Setup(r => r.UpdateAsync(list, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(list.Id, response.ListId);
        Assert.Equal(LibraryTestIds.BookId, response.BookId);
        Assert.Equal(1, response.BooksCount);
        Assert.Single(list.Items, item => item.BookId == LibraryTestIds.BookId);
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

        _bookSnapshotRepository.Verify(
            r => r.GetByBookIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userListRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BookHasNoSnapshot_ThrowsNotFoundException()
    {
        var list = UserListBuilder.Valid().Build();
        var command = AddBookToListCommandBuilder.Valid()
            .WithListId(list.Id)
            .WithBookId(LibraryTestIds.OtherBookId)
            .Build();

        _userListRepository
            .Setup(r => r.GetByIdAsync(list.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(LibraryTestIds.OtherBookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookSnapshot?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userListRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
