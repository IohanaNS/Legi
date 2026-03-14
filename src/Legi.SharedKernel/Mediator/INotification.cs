namespace Legi.SharedKernel.Mediator;

/// <summary>
/// Marker interface for notifications.
/// Unlike requests, notifications can have zero or many handlers.
/// Domain events implement this via IDomainEvent.
/// </summary>
public interface INotification;