using Legi.Identity.Application.Auth.Commands.RefreshToken;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.RefreshToken;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IJwtTokenService> _tokenServiceMock;
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _tokenServiceMock = new Mock<IJwtTokenService>();

        _handler = new RefreshTokenCommandHandler(
            _userRepositoryMock.Object,
            _tokenServiceMock.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldRefreshTokens_WhenRefreshTokenIsValid()
    {
        // Arrange
        var command = RefreshTokenCommandFactory.Create("old_refresh_token");
        var user = UserFactory.Create();
        user.AddRefreshToken(command.RefreshToken, DateTime.UtcNow.AddDays(1));
        var accessTokenExpiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        _userRepositoryMock
            .Setup(x => x.GetByRefreshTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(("new_access_token", accessTokenExpiresAt));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("new_access_token", result.Token);
        Assert.Equal("new_refresh_token", result.RefreshToken);
        Assert.Equal(accessTokenExpiresAt, result.ExpiresAt);

        Assert.Null(user.GetValidRefreshToken(command.RefreshToken));
        Assert.NotNull(user.GetValidRefreshToken("new_refresh_token"));

        _userRepositoryMock.Verify(x => x.UpdateAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenUserIsNotFound()
    {
        // Arrange
        var command = RefreshTokenCommandFactory.Create("invalid_token");

        _userRepositoryMock
            .Setup(x => x.GetByRefreshTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid or expired refresh token.", exception.Message);

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var command = RefreshTokenCommandFactory.Create("expired_token");
        var user = UserFactory.Create();
        user.AddRefreshToken(command.RefreshToken, DateTime.UtcNow.AddMinutes(-1));

        _userRepositoryMock
            .Setup(x => x.GetByRefreshTokenAsync(command.RefreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid or expired refresh token.", exception.Message);

        _tokenServiceMock.Verify(x => x.GenerateRefreshToken(), Times.Never);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }
}
