using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Commands.UpdateProfile;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Users.Commands.UpdateProfile;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly UpdateProfileCommandHandler _handler;

    public UpdateProfileCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _handler = new UpdateProfileCommandHandler(_userRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndPersistProfile_WhenUserExists()
    {
        // Arrange
        var command = UpdateProfileCommandFactory.Create();
        var user = UserFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(command.Name, result.Name);
        Assert.Equal(command.Bio, result.Bio);
        Assert.Equal(command.AvatarUrl, result.AvatarUrl);

        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotChangeName_WhenNameIsNull()
    {
        // Arrange
        var command = UpdateProfileCommandFactory.Create(name: null, bio: "new bio", avatarUrl: null);
        var user = UserFactory.Create(name: "Original Name");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("Original Name", result.Name);
        Assert.Equal("new bio", result.Bio);
    }

    [Fact]
    public async Task Handle_ShouldThrowApplicationException_WhenUserDoesNotExist()
    {
        // Arrange
        var command = UpdateProfileCommandFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<ApplicationException>(act);
        Assert.Equal("USER_NOT_FOUND", exception.Message);

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
