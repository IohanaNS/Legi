using Legi.Catalog.Application.Books.IntegrationEventHandlers;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Contracts.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.IntegrationEventHandlers;

public class UserBookRatedIntegrationEventHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepo = new();
    private readonly Mock<IBookRatingRepository> _ratingRepo = new();
    private readonly UserBookRatedIntegrationEventHandler _handler;

    public UserBookRatedIntegrationEventHandlerTests()
    {
        _handler = new UserBookRatedIntegrationEventHandler(
            _bookRepo.Object, _ratingRepo.Object,
            NullLogger<UserBookRatedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_AppliesRecomputedAggregateToTrackedBook_AndPassesCorrectRepoArgs()
    {
        var book = DomainBookFactory.Create();
        var userId = Guid.NewGuid();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _ratingRepo
            .Setup(r => r.StageRatingAsync(book.Id, userId, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookRatingAggregate(4.0m, 2));

        await _handler.Handle(
            new UserBookRatedIntegrationEvent(book.Id, userId, Rating: 8, PreviousRating: null),
            CancellationToken.None);

        // Handler applies exactly what the recompute returned onto the tracked Book.
        Assert.Equal(4.0m, book.AverageRating);
        Assert.Equal(2, book.RatingsCount);
        _ratingRepo.Verify(r => r.StageRatingAsync(book.Id, userId, 8, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AppliesWhateverAggregateRecomputeReturns_ReRateExample()
    {
        // A re-rate keeps count steady, moves average — the handler just applies the
        // aggregate the repository recomputes (the count-steady logic lives in the repo,
        // verified against a real DB in the integration test).
        var book = DomainBookFactory.Create();
        var userId = Guid.NewGuid();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _ratingRepo
            .Setup(r => r.StageRatingAsync(book.Id, userId, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BookRatingAggregate(4.5m, 1));

        await _handler.Handle(
            new UserBookRatedIntegrationEvent(book.Id, userId, Rating: 9, PreviousRating: 6),
            CancellationToken.None);

        Assert.Equal(4.5m, book.AverageRating);
        Assert.Equal(1, book.RatingsCount);
    }

    [Fact]
    public async Task Handle_BookNotFound_Throws_AndDoesNotTouchRatingRepo()
    {
        _bookRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(
            new UserBookRatedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), 8, null),
            CancellationToken.None));

        _ratingRepo.Verify(
            r => r.StageRatingAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
