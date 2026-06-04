using Legi.Catalog.Application.Books.IntegrationEventHandlers;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Library;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.IntegrationEventHandlers;

public class UserBookRatingRemovedIntegrationEventHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepo = new();
    private readonly Mock<IBookRatingRepository> _ratingRepo = new();
    private readonly UserBookRatingRemovedIntegrationEventHandler _handler;

    public UserBookRatingRemovedIntegrationEventHandlerTests()
    {
        _handler = new UserBookRatingRemovedIntegrationEventHandler(
            _bookRepo.Object, _ratingRepo.Object,
            NullLogger<UserBookRatingRemovedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_AppliesRecomputedAggregate_AfterRemoval()
    {
        var book = DomainBookFactory.Create();
        var userId = Guid.NewGuid();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _ratingRepo
            .Setup(r => r.StageRatingRemovalAsync(book.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookRatingAggregate(3.0m, 1));

        await _handler.Handle(
            new UserBookRatingRemovedIntegrationEvent(book.Id, userId, RemovedRating: 8),
            CancellationToken.None);

        Assert.Equal(3.0m, book.AverageRating);
        Assert.Equal(1, book.RatingsCount);
        _ratingRepo.Verify(r => r.StageRatingRemovalAsync(book.Id, userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LastRatingRemoved_ResetsBookToZero()
    {
        var book = DomainBookFactory.Create();
        var userId = Guid.NewGuid();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _ratingRepo
            .Setup(r => r.StageRatingRemovalAsync(book.Id, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookRatingAggregate(0m, 0));

        await _handler.Handle(
            new UserBookRatingRemovedIntegrationEvent(book.Id, userId, RemovedRating: 6),
            CancellationToken.None);

        Assert.Equal(0m, book.AverageRating);
        Assert.Equal(0, book.RatingsCount);
    }

    [Fact]
    public async Task Handle_BookNotFound_Throws_AndDoesNotTouchRatingRepo()
    {
        _bookRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        await Assert.ThrowsAsync<TransientMessagingException>(() => _handler.Handle(
            new UserBookRatingRemovedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), 6),
            CancellationToken.None));

        _ratingRepo.Verify(
            r => r.StageRatingRemovalAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
