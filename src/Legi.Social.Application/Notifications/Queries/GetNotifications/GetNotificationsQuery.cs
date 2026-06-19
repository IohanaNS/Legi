using Legi.SharedKernel.Mediator;
using Legi.Social.Application.Common.DTOs;

namespace Legi.Social.Application.Notifications.Queries.GetNotifications;

/// <summary>
/// Gets the authenticated user's notifications, newest first.
/// </summary>
public record GetNotificationsQuery(
    Guid RecipientId,
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedList<NotificationDto>>;
