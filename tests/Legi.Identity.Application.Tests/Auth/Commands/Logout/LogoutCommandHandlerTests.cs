using Legi.Identity.Application.Auth.Commands.Logout;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenService> _tokenServiceMock;
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<IJwtTokenService>();
        _handler = new LogoutCommandHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldRevokeRefreshTokenAndPersist_WhenTokenIsValid()
    {
        // Arrange
        var command = LogoutCommandFactory.Create(refreshToken: "valid_token");
        var user = UserFactory.Create();
        user.AddRefreshToken("valid_token_hash", DateTime.UtcNow.AddDays(1));

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken(command.RefreshToken))
            .Returns("valid_token_hash");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Null(user.GetValidRefreshToken("valid_token_hash"));
        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotThrowAndNotPersist_WhenUserDoesNotExist()
    {
        // Arrange
        var command = LogoutCommandFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Record.ExceptionAsync(act);
        Assert.Null(exception);
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldNotPersist_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var command = LogoutCommandFactory.Create(refreshToken: "missing_token");
        var user = UserFactory.Create();

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken(command.RefreshToken))
            .Returns("missing_token_hash");

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
