using Legi.Identity.Application.Common.Exceptions;
using Legi.Identity.Application.Common.Interfaces;
using Legi.Identity.Application.Tests.Factories;
using Legi.Identity.Application.Users.Commands.DeleteAccount;
using Legi.Identity.Domain.Entities;
using Legi.Identity.Domain.Events;
using Legi.Identity.Domain.Repositories;
using Moq;

namespace Legi.Identity.Application.Tests.Users.Commands.DeleteAccount;

public class DeleteAccountCommandHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ISecurityAuditLogger> _auditLoggerMock;
    private readonly DeleteAccountCommandHandler _handler;

    public DeleteAccountCommandHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _auditLoggerMock = new Mock<ISecurityAuditLogger>();
        _handler = new DeleteAccountCommandHandler(
            _userRepositoryMock.Object,
            _auditLoggerMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddDeletionEventAndDeleteUser_WhenUserExists()
    {
        // Arrange
        var command = DeleteAccountCommandFactory.Create();
        var user = UserFactory.Create();
        user.ClearDomainEvents();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Single(user.DomainEvents, e => e is UserDeletedDomainEvent);

        _userRepositoryMock.Verify(x => x.DeleteAsync(user, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldThrowNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var command = DeleteAccountCommandFactory.Create();

        _userRepositoryMock
            .Setup(x => x.GetByIdAsync(command.UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(act);
        Assert.Equal($"User with key '{command.UserId}' was not found.", exception.Message);

        _userRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
