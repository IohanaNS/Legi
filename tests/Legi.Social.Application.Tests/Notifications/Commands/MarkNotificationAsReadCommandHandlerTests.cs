using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Notifications.Commands.MarkNotificationAsRead;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Notifications.Commands;

public class MarkNotificationAsReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly MarkNotificationAsReadCommandHandler _handler;

    public MarkNotificationAsReadCommandHandlerTests()
    {
        _handler = new MarkNotificationAsReadCommandHandler(_notifications.Object);
    }

    private static Notification CreateFor(Guid recipientId) =>
        Notification.CreateLike(
            recipientId, Guid.NewGuid(), "carlos", null, InteractableType.Post, Guid.NewGuid());

    [Fact]
    public async Task Handle_MarksRead_WhenRecipientMatches()
    {
        var userId = Guid.NewGuid();
        var notification = CreateFor(userId);
        _notifications
            .Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        await _handler.Handle(
            new MarkNotificationAsReadCommand(userId, notification.Id), CancellationToken.None);

        Assert.True(notification.IsRead);
        _notifications.Verify(
            r => r.MarkAsReadAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Throws_WhenNotificationMissing()
    {
        _notifications
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Notification?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new MarkNotificationAsReadCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Throws_WhenRecipientIsNotCaller()
    {
        var notification = CreateFor(Guid.NewGuid());
        _notifications
            .Setup(r => r.GetByIdAsync(notification.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(notification);

        await Assert.ThrowsAsync<ForbiddenException>(() => _handler.Handle(
            new MarkNotificationAsReadCommand(Guid.NewGuid(), notification.Id), CancellationToken.None));

        _notifications.Verify(
            r => r.MarkAsReadAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
