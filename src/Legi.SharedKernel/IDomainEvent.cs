using Legi.SharedKernel.Mediator;

namespace Legi.SharedKernel;

/// <summary>
/// Marker interface for domain events.
/// Inherits INotification so events can be dispatched via Mediator.Publish().
/// </summary>
public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}