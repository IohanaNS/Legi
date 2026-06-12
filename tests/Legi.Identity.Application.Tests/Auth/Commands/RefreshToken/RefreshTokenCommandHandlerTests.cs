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
        var accessTokenExpiresAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var refreshTokenExpiresAt = new DateTime(2030, 1, 8, 0, 0, 0, DateTimeKind.Utc);

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken(command.RefreshToken))
            .Returns("old_refresh_token_hash");

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("new_refresh_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("new_refresh_token"))
            .Returns("new_refresh_token_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(refreshTokenExpiresAt);

        _userRepositoryMock
            .Setup(x => x.RotateRefreshTokenAsync(
                "old_refresh_token_hash",
                "new_refresh_token_hash",
                refreshTokenExpiresAt,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenRotationResult.Success(user));

        _tokenServiceMock
            .Setup(x => x.GenerateAccessToken(user))
            .Returns(("new_access_token", accessTokenExpiresAt));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.Email.Value, result.Email);
        Assert.Equal(user.Username.Value, result.Username);
        Assert.Equal("new_access_token", result.Token);
        Assert.Equal("new_refresh_token", result.RefreshToken);
        Assert.Equal(accessTokenExpiresAt, result.ExpiresAt);
        Assert.Equal(refreshTokenExpiresAt, result.RefreshTokenExpiresAt);

        _userRepositoryMock.Verify(
            x => x.RotateRefreshTokenAsync(
                "old_refresh_token_hash",
                "new_refresh_token_hash",
                refreshTokenExpiresAt,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenUserIsNotFound()
    {
        // Arrange
        var command = RefreshTokenCommandFactory.Create("invalid_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken(command.RefreshToken))
            .Returns("invalid_token_hash");

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("unused_refresh_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("unused_refresh_token"))
            .Returns("unused_refresh_token_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(DateTime.UtcNow.AddDays(14));

        _userRepositoryMock
            .Setup(x => x.RotateRefreshTokenAsync(
                "invalid_token_hash",
                "unused_refresh_token_hash",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenRotationResult.Invalid());

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid or expired refresh token.", exception.Message);

        _userRepositoryMock.Verify(
            x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedException_WhenRefreshTokenIsExpired()
    {
        // Arrange
        var command = RefreshTokenCommandFactory.Create("expired_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken(command.RefreshToken))
            .Returns("expired_token_hash");

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("unused_refresh_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("unused_refresh_token"))
            .Returns("unused_refresh_token_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(DateTime.UtcNow.AddDays(14));

        _userRepositoryMock
            .Setup(x => x.RotateRefreshTokenAsync(
                "expired_token_hash",
                "unused_refresh_token_hash",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenRotationResult.Invalid());

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid or expired refresh token.", exception.Message);

        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowUnauthorizedExceptionAndNotIssueAccessToken_WhenReplayIsDetected()
    {
        // Arrange
        var command = RefreshTokenCommandFactory.Create("replayed_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken(command.RefreshToken))
            .Returns("replayed_token_hash");

        _tokenServiceMock
            .Setup(x => x.GenerateRefreshToken())
            .Returns("unused_refresh_token");

        _tokenServiceMock
            .Setup(x => x.HashRefreshToken("unused_refresh_token"))
            .Returns("unused_refresh_token_hash");

        _tokenServiceMock
            .Setup(x => x.GetRefreshTokenExpiresAt())
            .Returns(DateTime.UtcNow.AddDays(14));

        _userRepositoryMock
            .Setup(x => x.RotateRefreshTokenAsync(
                "replayed_token_hash",
                "unused_refresh_token_hash",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenRotationResult.ReplayDetected());

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(act);
        Assert.Equal("Invalid or expired refresh token.", exception.Message);
        _tokenServiceMock.Verify(x => x.GenerateAccessToken(It.IsAny<User>()), Times.Never);
    }
}
