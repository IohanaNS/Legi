using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.ReadingPosts.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="ReadingProgressCreatedDomainEvent"/>
/// into the cross-context <see cref="ReadingPostCreatedIntegrationEvent"/> and
/// publishes it via <see cref="IEventBus"/>.
///
/// The domain entity is <c>ReadingProgress</c> internally; the cross-context
/// contract names it <c>ReadingPost</c> (the user-facing concept). Field name
/// translation happens here.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class ReadingProgressCreatedDomainEventHandler
    : INotificationHandler<ReadingProgressCreatedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ReadingProgressCreatedDomainEventHandler> _logger;

    public ReadingProgressCreatedDomainEventHandler(
        IEventBus eventBus,
        ILogger<ReadingProgressCreatedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        ReadingProgressCreatedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ReadingPostCreatedIntegrationEvent(
            PostId: domainEvent.ReadingPostId,
            UserId: domainEvent.UserId,
            BookId: domainEvent.BookId,
            Content: domainEvent.Content,
            ProgressValue: domainEvent.ProgressValue,
            ProgressType: domainEvent.ProgressType,
            IsSpoiler: domainEvent.IsSpoiler,
            CreatedAt: domainEvent.OccurredOn);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated ReadingProgressCreatedDomainEvent for post {PostId} to integration event",
            domainEvent.ReadingPostId);
    }
}
