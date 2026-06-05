using Legi.Contracts.Library;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Application.UserBooks.EventHandlers;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.UserBooks.EventHandlers;

public class BookAddedToLibraryDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBusMock = new();
    private readonly BookAddedToLibraryDomainEventHandler _handler;

    public BookAddedToLibraryDomainEventHandlerTests()
    {
        _handler = new BookAddedToLibraryDomainEventHandler(
            _eventBusMock.Object,
            NullLogger<BookAddedToLibraryDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesExactlyOneIntegrationEvent_WithMatchingFields()
    {
        // Arrange
        var domainEvent = LibraryDomainEventFactory.BookAddedToLibrary();

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<BookAddedToLibraryIntegrationEvent>(e =>
                    e.UserBookId == domainEvent.UserBookId &&
                    e.UserId == domainEvent.UserId &&
                    e.BookId == domainEvent.BookId &&
                    e.Wishlist == false &&
                    e.AddedAt == domainEvent.OccurredOn),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBusMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_PropagatesWishlistTrue()
    {
        // Arrange
        var domainEvent = LibraryDomainEventFactory.BookAddedToLibrary(wishList: true);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        _eventBusMock.Verify(
            x => x.PublishAsync(
                It.Is<BookAddedToLibraryIntegrationEvent>(e => e.Wishlist),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
