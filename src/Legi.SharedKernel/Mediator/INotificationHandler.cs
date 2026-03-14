namespace Legi.SharedKernel.Mediator;

/// <summary>
/// Defines a handler for a notification.
/// Unlike request handlers, multiple notification handlers can exist for the same notification type.
/// </summary>
/// <typeparam name="TNotification">The type of notification being handled</typeparam>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    Task Handle(TNotification notification, CancellationToken cancellationToken = default);
}