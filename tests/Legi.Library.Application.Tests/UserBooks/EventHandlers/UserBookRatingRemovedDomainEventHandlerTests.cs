using Legi.Contracts.Library;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.EventHandlers;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.EventHandlers;

public class UserBookRatingRemovedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly UserBookRatingRemovedDomainEventHandler _handler;

    public UserBookRatingRemovedDomainEventHandlerTests()
    {
        _handler = new UserBookRatingRemovedDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<UserBookRatingRemovedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesExactlyOneIntegrationEvent_WithRemovedRatingAsInt()
    {
        // Arrange
        var removed = Rating.Create(6); // 3.0 stars
        var domainEvent = LibraryDomainEventFactory.UserBookRatingRemoved(oldRating: removed);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<UserBookRatingRemovedIntegrationEvent>(e =>
                    e.BookId == domainEvent.BookId &&
                    e.UserId == domainEvent.UserId &&
                    e.RemovedRating == 6),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }
}
