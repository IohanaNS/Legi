using Legi.Identity.Application.Auth.Commands.ConfirmEmail;
using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Auth.Commands.ConfirmEmail;

public class ConfirmEmailCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<ISecureTokenFactory> _tokenFactoryMock = new();

    private ConfirmEmailCommandHandler CreateHandler() => new(
        _userRepositoryMock.Object,
        _tokenFactoryMock.Object);

    [Fact]
    public async Task Handle_ShouldConfirmEmail_WhenTokenIsValid()
    {
        // Arrange
        _tokenFactoryMock.Setup(x => x.Hash("raw-token")).Returns("token-hash");
        _userRepositoryMock
            .Setup(x => x.ConfirmEmailAsync("token-hash", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = CreateHandler();
        var command = new ConfirmEmailCommand("raw-token");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _userRepositoryMock.Verify(
            x => x.ConfirmEmailAsync("token-hash", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFound_WhenTokenIsInvalid()
    {
        // Arrange
        _tokenFactoryMock.Setup(x => x.Hash(It.IsAny<string>())).Returns("token-hash");
        _userRepositoryMock
            .Setup(x => x.ConfirmEmailAsync("token-hash", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var handler = CreateHandler();
        var command = new ConfirmEmailCommand("raw-token");

        // Act
        var act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<NotFoundException>(act);
    }
}
