using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Notifications.Commands.MarkNotificationAsRead;

public record MarkNotificationAsReadCommand(Guid UserId, Guid NotificationId) : IRequest;
