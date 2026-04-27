using Legi.SharedKernel.Mediator;

namespace Legi.Contracts;

/// <summary>
/// Marker interface for events that cross bounded context boundaries.
/// 
/// Inherits from <see cref="INotification"/> so consumers can handle integration
/// events with the existing <c>INotificationHandler&lt;T&gt;</c> mechanism — the
/// same pattern used for in-process domain events. From the handler's
/// perspective, an integration event is just another notification; it does not
/// know whether the event originated locally or arrived from RabbitMQ.
/// 
/// All integration events should be immutable records carrying primitive data
/// (no Value Objects from any domain). Each bounded context interprets the data
/// in its own context.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 3.2 and 6.1.
/// </summary>
public interface IIntegrationEvent : INotification
{
}