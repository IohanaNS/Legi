using Legi.Catalog.Application.Books.IntegrationEventHandlers;
using Legi.Catalog.Application.Tests.Factories;
using Legi.Catalog.Domain.Entities;
using Legi.Catalog.Domain.Repositories;
using Legi.Catalog.Domain.ValueObjects;
using Legi.Contracts.Library;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Catalog.Application.Tests.Books.IntegrationEventHandlers;

/// <summary>
/// The reviews count shown on the book page is the work's. These verify the Catalog
/// review handlers roll the count up to the work (via the book's WorkId) alongside
/// the per-edition Book count.
/// </summary>
public class ReviewsCountWorkRollupTests
{
    private readonly Mock<IBookRepository> _bookRepo = new();
    private readonly Mock<IWorkRepository> _workRepo = new();

    private static Work NewWork() => Work.Create(WorkKey.Synthesize("t", "a"), "t");

    [Fact]
    public async Task ReviewCreated_IncrementsBookAndWorkReviewsCount()
    {
        var book = DomainBookFactory.Create();
        var work = NewWork();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _workRepo.Setup(r => r.GetByIdAsync(book.WorkId, It.IsAny<CancellationToken>())).ReturnsAsync(work);

        var handler = new ReviewCreatedIntegrationEventHandler(
            _bookRepo.Object, _workRepo.Object, NullLogger<ReviewCreatedIntegrationEventHandler>.Instance);

        await handler.Handle(
            new ReviewCreatedIntegrationEvent(
                Guid.NewGuid(), Guid.NewGuid(), book.Id, "A thoughtful review.", 8, DateTime.UtcNow, WorkId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(1, book.ReviewsCount);
        Assert.Equal(1, work.ReviewsCount);
    }

    [Fact]
    public async Task ReadingPostDeleted_Review_DecrementsBookAndWorkReviewsCount()
    {
        var book = DomainBookFactory.Create();
        book.IncrementReviewsCount();
        var work = NewWork();
        work.IncrementReviewsCount();
        _bookRepo.Setup(r => r.GetByIdAsync(book.Id, It.IsAny<CancellationToken>())).ReturnsAsync(book);
        _workRepo.Setup(r => r.GetByIdAsync(book.WorkId, It.IsAny<CancellationToken>())).ReturnsAsync(work);

        var handler = new ReadingPostDeletedIntegrationEventHandler(
            _bookRepo.Object, _workRepo.Object, NullLogger<ReadingPostDeletedIntegrationEventHandler>.Instance);

        await handler.Handle(
            new ReadingPostDeletedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), book.Id, WorkId: Guid.NewGuid(), IsReview: true),
            CancellationToken.None);

        Assert.Equal(0, book.ReviewsCount);
        Assert.Equal(0, work.ReviewsCount);
    }

    [Fact]
    public async Task ReadingPostDeleted_NonReview_IsNoOp()
    {
        var handler = new ReadingPostDeletedIntegrationEventHandler(
            _bookRepo.Object, _workRepo.Object, NullLogger<ReadingPostDeletedIntegrationEventHandler>.Instance);

        await handler.Handle(
            new ReadingPostDeletedIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), WorkId: Guid.NewGuid(), IsReview: false),
            CancellationToken.None);

        _bookRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _workRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
