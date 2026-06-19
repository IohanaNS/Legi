using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Notifications.Queries.GetUnreadCount;

/// <summary>
/// Gets the number of unread notifications for the authenticated user (bell badge).
/// </summary>
public record GetUnreadCountQuery(Guid RecipientId) : IRequest<int>;
