using Legi.Contracts.Identity;
using Legi.Identity.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Users.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="UserDeletedDomainEvent"/> into the
/// cross-context <see cref="UserDeletedIntegrationEvent"/> and publishes it
/// via <see cref="IEventBus"/>.
///
/// Runs synchronously within the producer's SaveChangesAsync transaction
/// (via DispatchDomainEventsInterceptor). The integration event is staged
/// into the outbox table and committed together with the user deletion.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class UserDeletedDomainEventHandler
    : INotificationHandler<UserDeletedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserDeletedDomainEventHandler> _logger;

    public UserDeletedDomainEventHandler(
        IEventBus eventBus,
        ILogger<UserDeletedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        UserDeletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserDeletedIntegrationEvent(
            UserId: domainEvent.UserId,
            DeletedAt: domainEvent.OccurredOn);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated UserDeletedDomainEvent for user {UserId} to integration event",
            domainEvent.UserId);
    }
}
