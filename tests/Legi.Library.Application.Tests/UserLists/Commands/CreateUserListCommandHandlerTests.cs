using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserLists.Commands.CreateUserList;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Legi.SharedKernel;
using Moq;

namespace Legi.Library.Application.Tests.UserLists.Commands;

public class CreateUserListCommandHandlerTests
{
    private readonly Mock<IUserListRepository> _userListRepository = new();
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly CreateUserListCommandHandler _handler;

    public CreateUserListCommandHandlerTests()
    {
        _handler = new CreateUserListCommandHandler(
            _userListRepository.Object,
            _bookSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_UserBelowLimitAndNameAvailable_CreatesList()
    {
        var command = CreateUserListCommandBuilder.Valid()
            .Public()
            .Build();
        UserList? addedList = null;

        _userListRepository
            .Setup(r => r.GetCountByUserIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _userListRepository
            .Setup(r => r.ExistsByUserAndNameAsync(command.UserId, command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _userListRepository
            .Setup(r => r.AddAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()))
            .Callback<UserList, CancellationToken>((list, _) => addedList = list)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(addedList);
        Assert.Equal(command.UserId, addedList.UserId);
        Assert.Equal(command.Name, addedList.Name);
        Assert.Equal(command.Description, addedList.Description);
        Assert.True(addedList.IsPublic);
        Assert.Equal(addedList.Id, response.ListId);
        Assert.Equal(command.Name, response.Name);
        Assert.True(response.IsPublic);
    }

    [Fact]
    public async Task Handle_UserAtListLimit_ThrowsDomainException()
    {
        var command = CreateUserListCommandBuilder.Valid().Build();
        _userListRepository
            .Setup(r => r.GetCountByUserIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(100);

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userListRepository.Verify(
            r => r.AddAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NameAlreadyExists_ThrowsConflictException()
    {
        var command = CreateUserListCommandBuilder.Valid().Build();
        _userListRepository
            .Setup(r => r.GetCountByUserIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _userListRepository
            .Setup(r => r.ExistsByUserAndNameAsync(command.UserId, command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userListRepository.Verify(
            r => r.AddAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BookWithoutSnapshot_ThrowsNotFoundAndDoesNotCreate()
    {
        var command = CreateUserListCommandBuilder.Valid()
            .WithBooks(LibraryTestIds.BookId)
            .Build();

        _userListRepository
            .Setup(r => r.GetCountByUserIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _userListRepository
            .Setup(r => r.ExistsByUserAndNameAsync(command.UserId, command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(LibraryTestIds.BookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Legi.Library.Domain.Entities.BookSnapshot?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userListRepository.Verify(
            r => r.AddAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_BooksWithSnapshots_CreatesListWithItems()
    {
        var command = CreateUserListCommandBuilder.Valid()
            .WithBooks(LibraryTestIds.BookId, LibraryTestIds.OtherBookId)
            .Build();
        UserList? added = null;

        _userListRepository
            .Setup(r => r.GetCountByUserIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _userListRepository
            .Setup(r => r.ExistsByUserAndNameAsync(command.UserId, command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _bookSnapshotRepository
            .Setup(r => r.GetByBookIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => BookSnapshotFactory.Create(id));
        _userListRepository
            .Setup(r => r.AddAsync(It.IsAny<UserList>(), It.IsAny<CancellationToken>()))
            .Callback<UserList, CancellationToken>((list, _) => added = list)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(2, added.BooksCount);
        Assert.NotEqual(Guid.Empty, response.ListId);
    }
}
