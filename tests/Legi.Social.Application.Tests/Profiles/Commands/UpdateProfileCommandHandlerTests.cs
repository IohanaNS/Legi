using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Profiles.Commands.UpdateProfile;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Profiles.Commands;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _handler = new UpdateProfileCommandHandler(_userProfileRepository.Object);
    }

    [Fact]
    public async Task Handle_ProfileExists_UpdatesProfileAndReturnsResponse()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfileFactory.Create(userId, "reader");
        var command = new UpdateProfileCommand(
            userId,
            "New bio",
            "https://cdn.example.com/new-avatar.png",
            "https://cdn.example.com/new-banner.png");
        UserProfile? updatedProfile = null;

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _userProfileRepository
            .Setup(r => r.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()))
            .Callback<UserProfile, CancellationToken>((updated, _) => updatedProfile = updated)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Same(profile, updatedProfile);
        Assert.Equal(command.Bio, profile.Bio);
        Assert.Equal(command.AvatarUrl, profile.AvatarUrl);
        Assert.Equal(command.BannerUrl, profile.BannerUrl);
        Assert.Equal(profile.UserId, response.UserId);
        Assert.Equal(profile.Username, response.Username);
        Assert.Equal(command.Bio, response.Bio);
        Assert.Equal(command.AvatarUrl, response.AvatarUrl);
        Assert.Equal(command.BannerUrl, response.BannerUrl);
    }

    [Fact]
    public async Task Handle_ProfileMissing_ThrowsNotFoundException()
    {
        var command = new UpdateProfileCommand(
            Guid.NewGuid(),
            "New bio",
            null,
            null);
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userProfileRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
