using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="ReviewCreatedDomainEvent"/> into the
/// cross-context <see cref="ReviewCreatedIntegrationEvent"/> and publishes it via
/// <see cref="IEventBus"/>. Social consumes it for the feed fan-out (ReviewCreated)
/// and Catalog for the reviews count. Mirrors
/// <see cref="ReadingProgressCreatedDomainEventHandler"/>.
/// </summary>
public sealed class ReviewCreatedDomainEventHandler
    : INotificationHandler<ReviewCreatedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ReviewCreatedDomainEventHandler> _logger;

    public ReviewCreatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<ReviewCreatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        ReviewCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ReviewCreatedIntegrationEvent(
            ReviewId: domainEvent.ReviewId,
            UserId: domainEvent.UserId,
            BookId: domainEvent.BookId,
            Content: domainEvent.Content,
            Stars: domainEvent.Stars,
            IsSpoiler: domainEvent.IsSpoiler,
            CreatedAt: domainEvent.OccurredOn);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated ReviewCreatedDomainEvent for review {ReviewId} to integration event",
            domainEvent.ReviewId);
    }
}
