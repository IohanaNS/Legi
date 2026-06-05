using Legi.Contracts.Library;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.ReadingPosts.EventHandlers;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.EventHandlers;

public class ReadingPostDeletedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly ReadingPostDeletedDomainEventHandler _handler;

    public ReadingPostDeletedDomainEventHandlerTests()
    {
        _handler = new ReadingPostDeletedDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<ReadingPostDeletedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesIntegrationEvent_WithPostIdAndUserId()
    {
        // Arrange
        var domainEvent = LibraryDomainEventFactory.ReadingPostDeleted();

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<ReadingPostDeletedIntegrationEvent>(e =>
                    e.PostId == domainEvent.ReadingPostId &&
                    e.UserId == domainEvent.UserId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }
}
