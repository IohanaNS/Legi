using Legi.SharedKernel;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Follows.Commands.FollowUser;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Follows.Commands;

public class FollowUserCommandHandlerTests
{
    private readonly Mock<IFollowRepository> _followRepository = new();
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly FollowUserCommandHandler _handler;

    public FollowUserCommandHandlerTests()
    {
        _handler = new FollowUserCommandHandler(
            _followRepository.Object,
            _userProfileRepository.Object);
    }

    [Fact]
    public async Task Handle_TargetExistsAndNotAlreadyFollowing_AddsFollow()
    {
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();
        var command = new FollowUserCommand(followerId, followingId);
        Follow? addedFollow = null;

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(followingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfileFactory.Create(followingId, "target-reader"));
        _followRepository
            .Setup(r => r.GetByPairAsync(followerId, followingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Follow?)null);
        _followRepository
            .Setup(r => r.AddAsync(It.IsAny<Follow>(), It.IsAny<CancellationToken>()))
            .Callback<Follow, CancellationToken>((follow, _) => addedFollow = follow)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(addedFollow);
        Assert.Equal(followerId, addedFollow!.FollowerId);
        Assert.Equal(followingId, addedFollow.FollowingId);
        Assert.Equal(addedFollow.Id, response.FollowId);
        Assert.Equal(addedFollow.CreatedAt, response.CreatedAt);

        var domainEvent = Assert.IsType<FollowCreatedDomainEvent>(
            Assert.Single(addedFollow.DomainEvents));
        Assert.Equal(followerId, domainEvent.FollowerId);
        Assert.Equal(followingId, domainEvent.FollowingId);
    }

    [Fact]
    public async Task Handle_TargetProfileMissing_ThrowsNotFoundException()
    {
        var command = new FollowUserCommand(Guid.NewGuid(), Guid.NewGuid());
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(command.FollowingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _followRepository.Verify(
            r => r.GetByPairAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _followRepository.Verify(
            r => r.AddAsync(It.IsAny<Follow>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_FollowAlreadyExists_ThrowsConflictException()
    {
        var followerId = Guid.NewGuid();
        var followingId = Guid.NewGuid();
        var command = new FollowUserCommand(followerId, followingId);
        var existingFollow = FollowFactory.Create(followerId, followingId);

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(followingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfileFactory.Create(followingId));
        _followRepository
            .Setup(r => r.GetByPairAsync(followerId, followingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingFollow);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _followRepository.Verify(
            r => r.AddAsync(It.IsAny<Follow>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserFollowsThemselves_ThrowsDomainException()
    {
        var userId = Guid.NewGuid();
        var command = new FollowUserCommand(userId, userId);

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfileFactory.Create(userId));
        _followRepository
            .Setup(r => r.GetByPairAsync(userId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Follow?)null);

        await Assert.ThrowsAsync<DomainException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _followRepository.Verify(
            r => r.AddAsync(It.IsAny<Follow>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
