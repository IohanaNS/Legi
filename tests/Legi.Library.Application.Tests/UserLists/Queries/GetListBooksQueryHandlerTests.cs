using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserLists.Queries.GetListBooks;
using Moq;

namespace Legi.Library.Application.Tests.UserLists.Queries;

public class GetListBooksQueryHandlerTests
{
    private readonly Mock<IUserListReadRepository> _readRepository = new();
    private readonly GetListBooksQueryHandler _handler;

    public GetListBooksQueryHandlerTests()
    {
        _handler = new GetListBooksQueryHandler(_readRepository.Object);
    }

    [Fact]
    public async Task Handle_PublicList_ReturnsBooks()
    {
        var list = CreateList(isPublic: true);
        var page = CreateEmptyPage();

        _readRepository
            .Setup(r => r.GetDetailByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _readRepository
            .Setup(r => r.GetListBooksAsync(list.ListId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _handler.Handle(
            new GetListBooksQuery(list.ListId, LibraryTestIds.OtherUserId),
            CancellationToken.None);

        Assert.Same(page, result);
        _readRepository.Verify(
            r => r.GetListBooksAsync(list.ListId, 1, 20, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PrivateListOwnedByViewer_ReturnsBooks()
    {
        var list = CreateList(isPublic: false);
        var page = CreateEmptyPage();

        _readRepository
            .Setup(r => r.GetDetailByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _readRepository
            .Setup(r => r.GetListBooksAsync(list.ListId, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _handler.Handle(
            new GetListBooksQuery(list.ListId, LibraryTestIds.UserId),
            CancellationToken.None);

        Assert.Same(page, result);
    }

    [Fact]
    public async Task Handle_PrivateListOwnedByAnotherUser_ThrowsNotFoundAndDoesNotFetchBooks()
    {
        var list = CreateList(isPublic: false);

        _readRepository
            .Setup(r => r.GetDetailByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new GetListBooksQuery(list.ListId, LibraryTestIds.OtherUserId),
                CancellationToken.None));

        _readRepository.Verify(
            r => r.GetListBooksAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MissingList_ThrowsNotFoundAndDoesNotFetchBooks()
    {
        _readRepository
            .Setup(r => r.GetDetailByIdAsync(LibraryTestIds.UserListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserListDetailDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new GetListBooksQuery(LibraryTestIds.UserListId, LibraryTestIds.UserId),
                CancellationToken.None));

        _readRepository.Verify(
            r => r.GetListBooksAsync(
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static UserListDetailDto CreateList(bool isPublic) =>
        new(
            LibraryTestIds.UserListId,
            LibraryTestIds.UserId,
            "Favorites",
            "Books worth returning to.",
            isPublic,
            BooksCount: 0,
            LikesCount: 0,
            CommentsCount: 0,
            DateTime.UtcNow,
            DateTime.UtcNow);

    private static PaginatedList<UserListBookDto> CreateEmptyPage() =>
        new([], totalCount: 0, pageNumber: 1, pageSize: 20);
}
