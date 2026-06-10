using Legi.Library.Application.Common.DTOs;
using Legi.Library.Application.Common.Interfaces;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserLists.Queries.GetUserLists;
using Moq;

namespace Legi.Library.Application.Tests.UserLists.Queries;

public class GetUserListsQueryHandlerTests
{
    private readonly Mock<IUserListReadRepository> _readRepository = new();
    private readonly GetUserListsQueryHandler _handler;

    public GetUserListsQueryHandlerTests()
    {
        _handler = new GetUserListsQueryHandler(_readRepository.Object);
    }

    [Fact]
    public async Task Handle_TargetAndViewer_PassesThroughToVisibleListsReadRepository()
    {
        var page = new PaginatedList<UserListSummaryDto>(
            [],
            totalCount: 0,
            pageNumber: 2,
            pageSize: 10);

        _readRepository
            .Setup(r => r.GetVisibleByUserIdAsync(
                LibraryTestIds.UserId,
                LibraryTestIds.OtherUserId,
                2,
                10,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _handler.Handle(
            new GetUserListsQuery(
                LibraryTestIds.UserId,
                LibraryTestIds.OtherUserId,
                PageNumber: 2,
                PageSize: 10),
            CancellationToken.None);

        Assert.Same(page, result);
        _readRepository.Verify(
            r => r.GetVisibleByUserIdAsync(
                LibraryTestIds.UserId,
                LibraryTestIds.OtherUserId,
                2,
                10,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
