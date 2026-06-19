using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Common.Interfaces;

public interface INotificationReadRepository
{
    /// <summary>
    /// Gets the recipient's notifications, newest first, paginated.
    /// </summary>
    Task<PaginatedList<NotificationDto>> GetNotificationsAsync(
        Guid recipientId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Number of unread notifications for the recipient (for the bell badge).</summary>
    Task<int> GetUnreadCountAsync(
        Guid recipientId,
        CancellationToken cancellationToken = default);
}
