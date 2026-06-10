using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Exceptions;
using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Application.Common.Policies;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserLists.Queries.GetListDetails;
using Moq;

namespace Legi.Library.Application.Tests.UserLists.Queries;

public class GetListDetailsQueryHandlerTests
{
    private readonly Mock<IUserListReadRepository> _readRepository = new();
    private readonly GetListDetailsQueryHandler _handler;

    public GetListDetailsQueryHandlerTests()
    {
        _handler = new GetListDetailsQueryHandler(
            _readRepository.Object,
            new UserListVisibilityPolicy());
    }

    [Fact]
    public async Task Handle_PublicList_ReturnsListDetails()
    {
        var list = CreateList(isPublic: true);

        _readRepository
            .Setup(r => r.GetDetailByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _handler.Handle(
            new GetListDetailsQuery(list.ListId, LibraryTestIds.OtherUserId),
            CancellationToken.None);

        Assert.Equal(list.ListId, result.ListId);
        Assert.True(result.IsPublic);
    }

    [Fact]
    public async Task Handle_PrivateListOwnedByViewer_ReturnsListDetails()
    {
        var list = CreateList(isPublic: false);

        _readRepository
            .Setup(r => r.GetDetailByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        var result = await _handler.Handle(
            new GetListDetailsQuery(list.ListId, LibraryTestIds.UserId),
            CancellationToken.None);

        Assert.Equal(list.ListId, result.ListId);
        Assert.False(result.IsPublic);
    }

    [Fact]
    public async Task Handle_PrivateListOwnedByAnotherUser_ThrowsNotFoundException()
    {
        var list = CreateList(isPublic: false);

        _readRepository
            .Setup(r => r.GetDetailByIdAsync(list.ListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new GetListDetailsQuery(list.ListId, LibraryTestIds.OtherUserId),
                CancellationToken.None));
    }

    [Fact]
    public async Task Handle_MissingList_ThrowsNotFoundException()
    {
        _readRepository
            .Setup(r => r.GetDetailByIdAsync(LibraryTestIds.UserListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserListDetailDto?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new GetListDetailsQuery(LibraryTestIds.UserListId, LibraryTestIds.UserId),
                CancellationToken.None));
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
}
