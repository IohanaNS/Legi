using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.UserLists.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="UserListUpdatedDomainEvent"/> into the
/// cross-context <see cref="UserListUpdatedIntegrationEvent"/> and publishes it
/// via <see cref="IEventBus"/>. Social consumes it to keep the list
/// <c>ContentSnapshot</c> in sync with the current visibility.
/// </summary>
public sealed class UserListUpdatedDomainEventHandler(
    IEventBus eventBus,
    ILogger<UserListUpdatedDomainEventHandler> logger)
    : INotificationHandler<UserListUpdatedDomainEvent>
{
    public async Task Handle(
        UserListUpdatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserListUpdatedIntegrationEvent(
            ListId: domainEvent.UserListId,
            OwnerId: domainEvent.UserId,
            Name: domainEvent.Name,
            IsPublic: domainEvent.IsPublic);

        await eventBus.PublishAsync(integrationEvent, cancellationToken);

        logger.LogDebug(
            "Translated UserListUpdatedDomainEvent for list {ListId} to integration event",
            domainEvent.UserListId);
    }
}
