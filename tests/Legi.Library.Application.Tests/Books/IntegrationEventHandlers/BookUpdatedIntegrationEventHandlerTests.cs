using Legi.Library.Application.Books.IntegrationEventHandlers;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.Books.IntegrationEventHandlers;

public class BookUpdatedIntegrationEventHandlerTests
{
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly BookUpdatedIntegrationEventHandler _handler;

    public BookUpdatedIntegrationEventHandlerTests()
    {
        _handler = new BookUpdatedIntegrationEventHandler(
            _bookSnapshotRepository.Object,
            NullLogger<BookUpdatedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_BookUpdatedEvent_StagesSnapshotWithUpdatedFields()
    {
        var integrationEvent = CatalogIntegrationEventFactory.BookUpdated(
            title: "The Pragmatic Programmer",
            authors: ["Andrew Hunt", "David Thomas"],
            coverUrl: null,
            pageCount: 352);
        BookSnapshot? stagedSnapshot = null;
        _bookSnapshotRepository
            .Setup(r => r.StageAddOrUpdateAsync(It.IsAny<BookSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BookSnapshot, CancellationToken>((snapshot, _) => stagedSnapshot = snapshot)
            .Returns(Task.CompletedTask);

        await _handler.Handle(integrationEvent, CancellationToken.None);

        Assert.NotNull(stagedSnapshot);
        Assert.Equal(integrationEvent.BookId, stagedSnapshot.BookId);
        Assert.Equal("The Pragmatic Programmer", stagedSnapshot.Title);
        Assert.Equal("Andrew Hunt, David Thomas", stagedSnapshot.AuthorDisplay);
        Assert.Null(stagedSnapshot.CoverUrl);
        Assert.Equal(352, stagedSnapshot.PageCount);
        _bookSnapshotRepository.Verify(
            r => r.StageAddOrUpdateAsync(It.IsAny<BookSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
