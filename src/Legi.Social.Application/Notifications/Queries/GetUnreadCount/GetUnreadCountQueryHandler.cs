using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.Interfaces;

namespace Legi.Social.Application.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountQueryHandler(INotificationReadRepository notificationReadRepository)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(
        GetUnreadCountQuery request,
        CancellationToken cancellationToken)
    {
        return await notificationReadRepository.GetUnreadCountAsync(
            request.RecipientId, cancellationToken);
    }
}
