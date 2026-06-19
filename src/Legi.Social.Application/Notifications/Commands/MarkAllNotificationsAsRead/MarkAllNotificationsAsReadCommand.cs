using Legi.SharedKernel.Mediator;

namespace Legi.Social.Application.Notifications.Commands.MarkAllNotificationsAsRead;

public record MarkAllNotificationsAsReadCommand(Guid UserId) : IRequest;
