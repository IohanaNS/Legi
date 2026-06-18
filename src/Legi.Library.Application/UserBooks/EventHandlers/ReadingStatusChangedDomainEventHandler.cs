using Legi.Contracts.Library;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Legi.SharedKernel.Mediator;
using Microsoft.Extensions.Logging;

namespace Legi.Library.Application.UserBooks.EventHandlers;

/// <summary>
/// Translates the in-process <see cref="ReadingStatusChangedDomainEvent"/> into
/// the cross-context <see cref="ReadingStatusChangedIntegrationEvent"/> and
/// publishes it via <see cref="IEventBus"/>.
///
/// Status enums are serialized as strings at the boundary so the contract
/// isn't coupled to <c>Legi.Library.Domain.Enums.ReadingStatus</c>.
///
/// See MESSAGING-ARCHITECTURE-decisions.md, decisions 2.5 and 3.4.
/// </summary>
public sealed class ReadingStatusChangedDomainEventHandler
    : INotificationHandler<ReadingStatusChangedDomainEvent>
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ReadingStatusChangedDomainEventHandler> _logger;

    public ReadingStatusChangedDomainEventHandler(
        IEventBus eventBus,
        ILogger<ReadingStatusChangedDomainEventHandler> logger)
    {
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task Handle(
        ReadingStatusChangedDomainEvent domainEvent,
        CancellationToken cancellationToken)
    {
        var integrationEvent = new ReadingStatusChangedIntegrationEvent(
            UserId: domainEvent.UserId,
            BookId: domainEvent.BookId,
            OldStatus: domainEvent.OldStatus.ToString(),
            NewStatus: domainEvent.NewStatus.ToString(),
            ChangedAt: domainEvent.OccurredOn,
            WorkId: domainEvent.WorkId);

        await _eventBus.PublishAsync(integrationEvent, cancellationToken);

        _logger.LogDebug(
            "Translated ReadingStatusChangedDomainEvent for user {UserId}, book {BookId} ({OldStatus} -> {NewStatus}) to integration event",
            domainEvent.UserId,
            domainEvent.BookId,
            domainEvent.OldStatus,
            domainEvent.NewStatus);
    }
}
