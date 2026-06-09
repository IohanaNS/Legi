using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="ReadingPostDeletedDomainEvent"/> into
/// the cross-context <see cref="ReadingPostDeletedIntegrationEvent"/> and
/// publishes it via <see cref="IEventBus"/>.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class ReadingPostDeletedDomainEventHandler
    : INotificationHandler<ReadingPostDeletedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ReadingPostDeletedDomainEventHandler> _logger;

    public ReadingPostDeletedDomainEventHandler(
        IEventBus eventBus,
        ILogger<ReadingPostDeletedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        ReadingPostDeletedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ReadingPostDeletedIntegrationEvent(
            PostId: domainEvent.ReadingPostId,
            UserId: domainEvent.UserId,
            BookId: domainEvent.BookId,
            IsReview: domainEvent.IsReview);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated ReadingPostDeletedDomainEvent for post {PostId} to integration event",
            domainEvent.ReadingPostId);
    }
}
