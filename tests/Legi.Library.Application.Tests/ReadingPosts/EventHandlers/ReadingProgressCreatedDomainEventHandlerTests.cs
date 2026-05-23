using Legi.Contracts.Library;
using Legi.Library.Application.ReadingPosts.EventHandlers;
using Legi.Library.Domain.Events;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.EventHandlers;

public class ReadingProgressCreatedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly ReadingProgressCreatedDomainEventHandler _handler;

    public ReadingProgressCreatedDomainEventHandlerTests()
    {
        _handler = new ReadingProgressCreatedDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<ReadingProgressCreatedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesIntegrationEvent_WithContentAndProgress()
    {
        // Arrange
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var domainEvent = new ReadingProgressCreatedDomainEvent(
            readingPostId: postId,
            userBookId: Guid.NewGuid(),
            userId: userId,
            bookId: bookId,
            content: "Halfway through, love it",
            progressValue: 50,
            progressType: "Percentage");

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<ReadingPostCreatedIntegrationEvent>(e =>
                    e.PostId == postId &&
                    e.UserId == userId &&
                    e.BookId == bookId &&
                    e.Content == "Halfway through, love it" &&
                    e.ProgressValue == 50 &&
                    e.ProgressType == "Percentage" &&
                    e.CreatedAt == domainEvent.OccurredOn),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_PreservesNulls_WhenContentOrProgressMissing()
    {
        // Content-only post (no progress)
        var domainEvent = new ReadingProgressCreatedDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            content: "Just a thought, no progress yet",
            progressValue: null,
            progressType: null);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<ReadingPostCreatedIntegrationEvent>(e =>
                    e.ProgressValue == null &&
                    e.ProgressType == null &&
                    e.Content == "Just a thought, no progress yet"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
