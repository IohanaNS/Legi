using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Follows.Commands.UnfollowUser;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Follows.Commands;

public class UnfollowUserCommandHandlerTests
{
    private readonly Mock<IFollowRepository> _followRepository = new();
    private readonly UnfollowUserCommandHandler _handler;

    public UnfollowUserCommandHandlerTests()
    {
        _handler = new UnfollowUserCommandHandler(_followRepository.Object);
    }

    [Fact]
    public async Task Handle_FollowExists_MarksForRemovalAndDeletes()
    {
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();
        var follow = FollowFactory.Create(followerId, followingId);
        follow.ClearDomainEvents();

        _followRepository
            .Setup(r => r.GetByPairAsync(followerId, followingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(follow);

        await _handler.Handle(new UnfollowUserCommand(followerId, followingId), CancellationToken.None);

        var domainEvent = Assert.IsType<FollowRemovedDomainEvent>(
            Assert.Single(follow.DomainEvents));
        Assert.Equal(followerId, domainEvent.FollowerId);
        Assert.Equal(followingId, domainEvent.FollowingId);

        _followRepository.Verify(
            r => r.DeleteAsync(follow, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FollowMissing_ThrowsNotFoundException()
    {
        var command = new UnfollowUserCommand(Guid.NewGuid(), Guid.NewGuid());
        _followRepository
            .Setup(r => r.GetByPairAsync(command.FollowerId, command.FollowingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Follow?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _followRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Follow>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
