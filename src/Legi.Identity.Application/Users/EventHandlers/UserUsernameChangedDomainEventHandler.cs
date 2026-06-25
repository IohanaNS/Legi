using Legi.Contracts.Identity;
using Legi.Identity.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Identity.Application.Users.EventHandlers;

public sealed class UserUsernameChangedDomainEventHandler
    : INotificationHandler<UserUsernameChangedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<UserUsernameChangedDomainEventHandler> _logger;

    public UserUsernameChangedDomainEventHandler(
        IEventBus eventBus,
        ILogger<UserUsernameChangedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        UserUsernameChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserUsernameChangedIntegrationEvent(
            UserId: domainEvent.UserId,
            NewUsername: domainEvent.NewUsername);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated UserUsernameChangedDomainEvent for user {UserId} to integration event",
            domainEvent.UserId);
    }
}
