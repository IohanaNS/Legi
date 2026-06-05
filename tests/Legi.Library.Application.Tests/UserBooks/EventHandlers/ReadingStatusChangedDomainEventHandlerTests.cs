using Legi.Contracts.Library;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.EventHandlers;
using Legi.Library.Domain.Enums;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.EventHandlers;

public class ReadingStatusChangedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly ReadingStatusChangedDomainEventHandler _handler;

    public ReadingStatusChangedDomainEventHandlerTests()
    {
        _handler = new ReadingStatusChangedDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<ReadingStatusChangedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesExactlyOneIntegrationEvent_WithStringifiedStatuses()
    {
        // Arrange
        var domainEvent = LibraryDomainEventFactory.ReadingStatusChanged(
            oldStatus: ReadingStatus.Reading,
            newStatus: ReadingStatus.Finished);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<ReadingStatusChangedIntegrationEvent>(e =>
                    e.UserId == domainEvent.UserId &&
                    e.BookId == domainEvent.BookId &&
                    e.OldStatus == "Reading" &&
                    e.NewStatus == "Finished" &&
                    e.ChangedAt == domainEvent.OccurredOn),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }

    [Fact]
    public void NoOpStatusChange_DoesNotRaiseDomainEvent_SoTranslatorIsNeverInvoked()
    {
        // The translator can only be invoked when the domain event is raised.
        // ChangeReadingStatus early-returns when the status is unchanged, so the
        // domain event is never raised and the translator never runs.
        var userBook = UserBookBuilder.Valid().Build();
        userBook.ClearDomainEvents();

        userBook.ChangeReadingStatus(ReadingStatus.NotStarted);

        Assert.Empty(userBook.DomainEvents);
    }
}
