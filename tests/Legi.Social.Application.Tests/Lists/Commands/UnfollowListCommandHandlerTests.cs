using Legi.Social.Application.Lists.Commands.UnfollowList;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Lists.Commands;

public class UnfollowListCommandHandlerTests
{
    private readonly Mock<IListFollowRepository> _listFollowRepository = new();
    private readonly UnfollowListCommandHandler _handler;

    public UnfollowListCommandHandlerTests()
    {
        _handler = new UnfollowListCommandHandler(_listFollowRepository.Object);
    }

    [Fact]
    public async Task Handle_Following_DeletesFollow()
    {
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var follow = ListFollow.Create(userId, listId);

        _listFollowRepository
            .Setup(r => r.GetByUserAndListAsync(userId, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(follow);

        await _handler.Handle(new UnfollowListCommand(userId, listId), CancellationToken.None);

        _listFollowRepository.Verify(
            r => r.DeleteAsync(follow, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFollowing_IsNoOp()
    {
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        _listFollowRepository
            .Setup(r => r.GetByUserAndListAsync(userId, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ListFollow?)null);

        await _handler.Handle(new UnfollowListCommand(userId, listId), CancellationToken.None);

        _listFollowRepository.Verify(
            r => r.DeleteAsync(It.IsAny<ListFollow>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
