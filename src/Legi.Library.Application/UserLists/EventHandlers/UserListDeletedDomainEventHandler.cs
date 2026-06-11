using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.UserLists.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="UserListDeletedDomainEvent"/> into the
/// cross-context <see cref="UserListDeletedIntegrationEvent"/> and publishes it
/// via <see cref="IEventBus"/>. Social consumes it to purge the list
/// <c>ContentSnapshot</c> and any associated likes, comments, and follows.
/// </summary>
public sealed class UserListDeletedDomainEventHandler(
    IEventBus eventBus,
    ILogger<UserListDeletedDomainEventHandler> logger)
    : INotificationHandler<UserListDeletedDomainEvent>
{
    public async Task Handle(
        UserListDeletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserListDeletedIntegrationEvent(
            ListId: domainEvent.UserListId,
            OwnerId: domainEvent.UserId);

        await eventBus.PublishAsync(integrationEvent, cancellationToken);

        logger.LogDebug(
            "Translated UserListDeletedDomainEvent for list {ListId} to integration event",
            domainEvent.UserListId);
    }
}
