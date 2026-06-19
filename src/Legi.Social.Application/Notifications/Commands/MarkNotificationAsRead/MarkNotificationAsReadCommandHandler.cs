using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Repositories;

namespace Legi.Social.Application.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommandHandler(INotificationRepository notificationRepository)
    : IRequestHandler<MarkNotificationAsReadCommand>
{
    public async Task Handle(
        MarkNotificationAsReadCommand request,
        CancellationToken cancellationToken)
    {
        var notification = await notificationRepository.GetByIdAsync(
            request.NotificationId, cancellationToken);
        if (notification is null)
            throw new NotFoundException(nameof(Notification), request.NotificationId);

        if (notification.RecipientId != request.UserId)
            throw new ForbiddenException("You can only mark your own notifications as read.");

        notification.MarkAsRead();
        await notificationRepository.MarkAsReadAsync(notification, cancellationToken);
    }
}
