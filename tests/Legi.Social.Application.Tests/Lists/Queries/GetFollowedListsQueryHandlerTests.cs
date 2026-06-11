using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;
using Legi.Social.Application.Lists.Queries.GetFollowedLists;
using Moq;

namespace Legi.Social.Application.Tests.Lists.Queries;

public class GetFollowedListsQueryHandlerTests
{
    private readonly Mock<IListSocialReadRepository> _readRepository = new();
    private readonly GetFollowedListsQueryHandler _handler;

    public GetFollowedListsQueryHandlerTests()
    {
        _handler = new GetFollowedListsQueryHandler(_readRepository.Object);
    }

    [Fact]
    public async Task Handle_PassesUserAndPagingThroughToReadRepository()
    {
        var userId = Guid.NewGuid();
        var page = new PaginatedList<FollowedListDto>(
            [new FollowedListDto(Guid.NewGuid(), DateTime.UtcNow)],
            totalItems: 1,
            page: 2,
            pageSize: 10);

        _readRepository
            .Setup(r => r.GetFollowedListsAsync(userId, 2, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(page);

        var result = await _handler.Handle(
            new GetFollowedListsQuery(userId, Page: 2, PageSize: 10),
            CancellationToken.None);

        Assert.Same(page, result);
        _readRepository.Verify(
            r => r.GetFollowedListsAsync(userId, 2, 10, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
