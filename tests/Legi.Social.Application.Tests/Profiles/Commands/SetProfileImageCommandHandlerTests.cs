using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Common.Storage;
using Legi.Social.Application.Profiles.Commands.SetProfileImage;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Profiles.Commands;

public class SetProfileImageCommandHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IObjectStorage> _objectStorage = new();
    private readonly SetProfileImageCommandHandler _handler;

    public SetProfileImageCommandHandlerTests()
    {
        _handler = new SetProfileImageCommandHandler(
            _userProfileRepository.Object,
            _objectStorage.Object);
    }

    [Fact]
    public async Task Handle_Avatar_PersistsUrlAndDeletesPreviousObject()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfileFactory.CreateDetailed(
            userId,
            avatarUrl: "/media/avatars/old.webp");
        var command = new SetProfileImageCommand(
            userId,
            ProfileImageKind.Avatar,
            "/media/avatars/new.webp");

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.Url, response.Url);
        Assert.Equal(command.Url, profile.AvatarUrl);
        _userProfileRepository.Verify(
            r => r.UpdateAsync(profile, It.IsAny<CancellationToken>()), Times.Once);
        _objectStorage.Verify(
            s => s.DeleteByUrlAsync("/media/avatars/old.webp", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Banner_UpdatesBannerAndLeavesAvatarUntouched()
    {
        var userId = Guid.NewGuid();
        var profile = UserProfileFactory.CreateDetailed(
            userId,
            avatarUrl: "/media/avatars/keep.webp",
            bannerUrl: null);
        var command = new SetProfileImageCommand(
            userId,
            ProfileImageKind.Banner,
            "/media/banners/new.webp");

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(command.Url, profile.BannerUrl);
        Assert.Equal("/media/avatars/keep.webp", profile.AvatarUrl);
        // No previous banner → nothing to delete.
        _objectStorage.Verify(
            s => s.DeleteByUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ProfileMissing_ThrowsNotFound()
    {
        var command = new SetProfileImageCommand(
            Guid.NewGuid(),
            ProfileImageKind.Avatar,
            "/media/avatars/new.webp");

        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _userProfileRepository.Verify(
            r => r.UpdateAsync(It.IsAny<UserProfile>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
