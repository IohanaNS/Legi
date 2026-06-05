using Legi.Library.Application.Books.IntegrationEventHandlers;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.Books.IntegrationEventHandlers;

public class BookCreatedIntegrationEventHandlerTests
{
    private readonly Mock<IBookSnapshotRepository> _bookSnapshotRepository = new();
    private readonly BookCreatedIntegrationEventHandler _handler;

    public BookCreatedIntegrationEventHandlerTests()
    {
        _handler = new BookCreatedIntegrationEventHandler(
            _bookSnapshotRepository.Object,
            NullLogger<BookCreatedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_BookCreatedEvent_StagesSnapshotWithJoinedAuthors()
    {
        var integrationEvent = CatalogIntegrationEventFactory.BookCreated(
            authors: ["Robert C. Martin", "Micah Martin"],
            pageCount: 464);
        BookSnapshot? stagedSnapshot = null;
        _bookSnapshotRepository
            .Setup(r => r.StageAddOrUpdateAsync(It.IsAny<BookSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<BookSnapshot, CancellationToken>((snapshot, _) => stagedSnapshot = snapshot)
            .Returns(Task.CompletedTask);

        await _handler.Handle(integrationEvent, CancellationToken.None);

        Assert.NotNull(stagedSnapshot);
        Assert.Equal(integrationEvent.BookId, stagedSnapshot.BookId);
        Assert.Equal(integrationEvent.Title, stagedSnapshot.Title);
        Assert.Equal("Robert C. Martin, Micah Martin", stagedSnapshot.AuthorDisplay);
        Assert.Equal(integrationEvent.CoverUrl, stagedSnapshot.CoverUrl);
        Assert.Equal(464, stagedSnapshot.PageCount);
        _bookSnapshotRepository.Verify(
            r => r.StageAddOrUpdateAsync(It.IsAny<BookSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
