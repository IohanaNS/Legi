using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler(INotificationReadRepository notificationReadRepository)
    : IRequestHandler<GetNotificationsQuery, PaginatedList<NotificationDto>>
{
    public async Task<PaginatedList<NotificationDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        return await notificationReadRepository.GetNotificationsAsync(
            request.RecipientId,
            request.Page,
            request.PageSize,
            cancellationToken);
    }
}
