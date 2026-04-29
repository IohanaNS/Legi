using Legi.Contracts.Identity;
using Legi.Identity.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Auth.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="UserRegisteredDomainEvent"/> into
/// the cross-context <see cref="UserRegisteredIntegrationEvent"/> and
/// publishes it via <see cref="IEventBus"/>.
/// 
/// Runs synchronously within the producer's SaveChangesAsync transaction
/// (via DispatchDomainEventsInterceptor). The integration event is staged
/// into the outbox table and committed together with the new user.
/// 
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class UserRegisteredDomainEventHandler
    : INotificationHandler<UserRegisteredDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserRegisteredDomainEventHandler> _logger;

    public UserRegisteredDomainEventHandler(
        IEventBus eventBus,
        ILogger<UserRegisteredDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        UserRegisteredDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserRegisteredIntegrationEvent(
            UserId: domainEvent.UserId,
            Username: domainEvent.Username,
            Email: domainEvent.Email,
            RegisteredAt: domainEvent.OccurredOn);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated UserRegisteredDomainEvent for user {UserId} to integration event",
            domainEvent.UserId);
    }
}