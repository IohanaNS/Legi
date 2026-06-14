using Legi.Identity.Application.Auth.Commands.ResetPassword;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Repositories;
using Legi.SharedKernel;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IPasswordResetTokenFactory> _tokenFactoryMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();

    private ResetPasswordCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenFactoryMock.Object,
        _passwordHasherMock.Object);

    [Fact]
    public async Task Handle_ShouldResetPassword_WhenTokenIsValid()
    {
        // Arrange
        _tokenFactoryMock.Setup(x => x.Hash("raw-token")).Returns("token-hash");
        _userRepositoryMock
            .Setup(x => x.RedeemPasswordResetTokenAsync(
                "token-hash",
                "new-hash",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _passwordHasherMock.Setup(x => x.Hash("NewPass123")).Returns("new-hash");

        var handler = CreateHandler();
        var command = new ResetPasswordCommand("raw-token", "NewPass123");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.RedeemPasswordResetTokenAsync(
                "token-hash",
                "new-hash",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenTokenDoesNotMatchAnyUser()
    {
        // Arrange
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("token-hash");
        _passwordHasherMock.Setup(x => x.Hash("NewPass123")).Returns("new-hash");
        _userRepositoryMock
            .Setup(x => x.RedeemPasswordResetTokenAsync(
                "token-hash",
                "new-hash",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new ResetPasswordCommand("raw-token", "NewPass123");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenTokenIsExpired()
    {
        // Arrange
        _tokenFactoryMock.Setup(x => x.Hash("raw-token")).Returns("token-hash");
        _passwordHasherMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("new-hash");
        _userRepositoryMock
            .Setup(x => x.RedeemPasswordResetTokenAsync(
                "token-hash",
                "new-hash",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DomainException("Invalid or expired reset token"));

        var handler = CreateHandler();
        var command = new ResetPasswordCommand("raw-token", "NewPass123");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<DomainException>(act);
        _userRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
