using Legi.Catalog.Application.Books.EventHandlers;
using Legi.Catalog.Domain.Events;
using Legi.Contracts.Catalog;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.EventHandlers;

public class BookCreatedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly BookCreatedDomainEventHandler _handler;

    public BookCreatedDomainEventHandlerTests()
    {
        _handler = new BookCreatedDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<BookCreatedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ShouldPublishIntegrationEvent_WithSnapshotPayload()
    {
        // Arrange
        var bookId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var createdByUserId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var workId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var domainEvent = new BookCreatedDomainEvent(
            bookId,
            "9780132350884",
            "Clean Code",
            ["Robert C. Martin", "Martin Fowler"],
            "https://example.com/clean-code.jpg",
            464,
            createdByUserId,
            workId);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<BookCreatedIntegrationEvent>(e =>
                    e.BookId == bookId &&
                    e.Isbn == "9780132350884" &&
                    e.Title == "Clean Code" &&
                    e.Authors.SequenceEqual(domainEvent.Authors) &&
                    e.CoverUrl == "https://example.com/clean-code.jpg" &&
                    e.PageCount == 464 &&
                    e.WorkId == workId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }
}
