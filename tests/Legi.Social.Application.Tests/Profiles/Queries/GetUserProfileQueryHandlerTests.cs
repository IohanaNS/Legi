using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Profiles.Queries.GetUserProfile;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Profiles.Queries;

public class GetUserProfileQueryHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IFollowRepository> _followRepository = new();
    private readonly GetUserProfileQueryHandler _handler;

    public GetUserProfileQueryHandlerTests()
    {
        _handler = new GetUserProfileQueryHandler(
            _userProfileRepository.Object,
            _followRepository.Object);
    }

    [Fact]
    public async Task Handle_ProfileExistsAndViewerFollowsTarget_ReturnsProfileWithFollowingFlag()
    {
        var targetUserId = Guid.NewGuid();
        var viewerUserId = Guid.NewGuid();
        var profile = UserProfileFactory.CreateDetailed(
            userId: targetUserId,
            username: "target",
            bio: "Target bio",
            followersCount: 2,
            followingCount: 3);
        var follow = FollowFactory.Create(viewerUserId, targetUserId);

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _followRepository
            .Setup(r => r.GetByPairAsync(viewerUserId, targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(follow);

        var result = await _handler.Handle(
            new GetUserProfileQuery(targetUserId, viewerUserId),
            CancellationToken.None);

        Assert.Equal(targetUserId, result.UserId);
        Assert.Equal("target", result.Username);
        Assert.Equal("Target bio", result.Bio);
        Assert.Equal(2, result.FollowersCount);
        Assert.Equal(3, result.FollowingCount);
        Assert.True(result.IsFollowing);
        Assert.Equal(profile.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public async Task Handle_ViewerIsTarget_DoesNotCheckFollowRelationship()
    {
        var targetUserId = Guid.NewGuid();
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfileFactory.Create(targetUserId));

        var result = await _handler.Handle(
            new GetUserProfileQuery(targetUserId, targetUserId),
            CancellationToken.None);

        Assert.False(result.IsFollowing);
        _followRepository.Verify(
            r => r.GetByPairAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ProfileMissing_ThrowsNotFoundException()
    {
        var targetUserId = Guid.NewGuid();
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetUserProfileQuery(targetUserId, Guid.NewGuid()), CancellationToken.None));

        _followRepository.Verify(
            r => r.GetByPairAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
