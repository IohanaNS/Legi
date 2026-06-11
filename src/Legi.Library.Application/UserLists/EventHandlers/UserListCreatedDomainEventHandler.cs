using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.UserLists.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="UserListCreatedDomainEvent"/> into the
/// cross-context <see cref="UserListCreatedIntegrationEvent"/> and publishes it
/// via <see cref="IEventBus"/>. Social consumes it to create a list
/// <c>ContentSnapshot</c> when the list is public.
/// </summary>
public sealed class UserListCreatedDomainEventHandler(
    IEventBus eventBus,
    ILogger<UserListCreatedDomainEventHandler> logger)
    : INotificationHandler<UserListCreatedDomainEvent>
{
    public async Task Handle(
        UserListCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new UserListCreatedIntegrationEvent(
            ListId: domainEvent.UserListId,
            OwnerId: domainEvent.UserId,
            Name: domainEvent.Name,
            IsPublic: domainEvent.IsPublic);

        await eventBus.PublishAsync(integrationEvent, cancellationToken);

        logger.LogDebug(
            "Translated UserListCreatedDomainEvent for list {ListId} to integration event",
            domainEvent.UserListId);
    }
}
