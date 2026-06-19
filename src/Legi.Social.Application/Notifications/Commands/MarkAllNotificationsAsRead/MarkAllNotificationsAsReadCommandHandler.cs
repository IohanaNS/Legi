using Legi.SharedKernel.Mediator;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Notifications.Commands.MarkAllNotificationsAsRead;

public class MarkAllNotificationsAsReadCommandHandler(INotificationRepository notificationRepository)
    : IRequestHandler<MarkAllNotificationsAsReadCommand>
{
    public async Task Handle(
        MarkAllNotificationsAsReadCommand request,
        CancellationToken cancellationToken)
    {
        // Filtered by recipient, so no cross-user leakage is possible.
        await notificationRepository.MarkAllAsReadAsync(request.UserId, cancellationToken);
    }
}
