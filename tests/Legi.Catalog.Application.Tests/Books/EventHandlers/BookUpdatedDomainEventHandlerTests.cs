using Legi.Catalog.Application.Books.EventHandlers;
using Legi.Catalog.Domain.Events;
using Legi.Contracts.Catalog;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.EventHandlers;

public class BookUpdatedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly BookUpdatedDomainEventHandler _handler;

    public BookUpdatedDomainEventHandlerTests()
    {
        _handler = new BookUpdatedDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<BookUpdatedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldPublishIntegrationEvent_WithSnapshotPayload()
    {
        // Arrange
        var bookId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var workId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var domainEvent = new BookUpdatedDomainEvent(
            bookId,
            "9780132350884",
            "Refactoring",
            ["Martin Fowler"],
            null,
            448,
            workId);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<BookUpdatedIntegrationEvent>(e =>
                    e.BookId == bookId &&
                    e.Isbn == "9780132350884" &&
                    e.Title == "Refactoring" &&
                    e.Authors.SequenceEqual(domainEvent.Authors) &&
                    e.CoverUrl == null &&
                    e.PageCount == 448 &&
                    e.WorkId == workId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }
}
