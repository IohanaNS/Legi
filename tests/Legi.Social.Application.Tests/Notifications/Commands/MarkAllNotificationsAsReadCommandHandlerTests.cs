using Legi.Social.Application.Notifications.Commands.MarkAllNotificationsAsRead;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Notifications.Commands;

public class MarkAllNotificationsAsReadCommandHandlerTests
{
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly MarkAllNotificationsAsReadCommandHandler _handler;

    public MarkAllNotificationsAsReadCommandHandlerTests()
    {
        _handler = new MarkAllNotificationsAsReadCommandHandler(_notifications.Object);
    }

    [Fact]
    public async Task Handle_DelegatesWithCallerId()
    {
        var userId = Guid.NewGuid();

        await _handler.Handle(
            new MarkAllNotificationsAsReadCommand(userId), CancellationToken.None);

        _notifications.Verify(
            r => r.MarkAllAsReadAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
