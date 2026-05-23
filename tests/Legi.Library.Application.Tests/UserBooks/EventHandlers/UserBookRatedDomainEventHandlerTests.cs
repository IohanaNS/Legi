using Legi.Contracts.Library;
using Legi.Library.Application.UserBooks.EventHandlers;
using Legi.Library.Domain.Events;
using Legi.Library.Domain.ValueObjects;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.EventHandlers;

public class UserBookRatedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly UserBookRatedDomainEventHandler _handler;

    public UserBookRatedDomainEventHandlerTests()
    {
        _handler = new UserBookRatedDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<UserBookRatedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesIntegrationEvent_WithRatingValuesAsInts_AndNullPrevious()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var newRating = Rating.Create(8); // 4.0 stars
        var domainEvent = new UserBookRatedDomainEvent(userId, bookId, oldRating: null, newRating);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<UserBookRatedIntegrationEvent>(e =>
                    e.UserId == userId &&
                    e.BookId == bookId &&
                    e.Rating == 8 &&
                    e.PreviousRating == null),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_MapsPreviousRating_WhenOldRatingIsSet()
    {
        // Arrange
        var domainEvent = new UserBookRatedDomainEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            oldRating: Rating.Create(6),
            newRating: Rating.Create(9));

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<UserBookRatedIntegrationEvent>(e => e.Rating == 9 && e.PreviousRating == 6),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
