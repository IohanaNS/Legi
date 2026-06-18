using Legi.Contracts.Library;
using Legi.Social.Application.Feed.IntegrationEventHandlers;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Feed.IntegrationEventHandlers;

public class ReadingStatusChangedIntegrationEventHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IBookSnapshotRepository> _books = new();
    private readonly Mock<IFeedItemRepository> _feed = new();
    private readonly ReadingStatusChangedIntegrationEventHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();

    public ReadingStatusChangedIntegrationEventHandlerTests()
    {
        _handler = new ReadingStatusChangedIntegrationEventHandler(
            _profiles.Object, _books.Object, _feed.Object,
            NullLogger<ReadingStatusChangedIntegrationEventHandler>.Instance);

        _profiles.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(_userId, "bob"));
        _books.Setup(r => r.GetByBookIdAsync(_bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshot.Create(_bookId, "Solaris", "Stanisław Lem", null, null));
    }

    [Fact]
    public async Task Handle_Finished_StagesBookFinishedFeedItem()
    {
        FeedItem? captured = null;
        _feed.Setup(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((fi, _) => captured = fi)
            .Returns(Task.CompletedTask);

        var evt = new ReadingStatusChangedIntegrationEvent(
            _userId, _bookId, OldStatus: "Reading", NewStatus: "Finished", ChangedAt: DateTime.UtcNow, WorkId: Guid.NewGuid());

        await _handler.Handle(evt, CancellationToken.None);

        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(ActivityType.BookFinished, captured!.ActivityType);
        Assert.Null(captured.TargetType);
        Assert.Equal(_bookId, captured.ReferenceId);
        Assert.Equal("Solaris", captured.BookTitle);
        Assert.Equal("bob", captured.ActorUsername);
        Assert.Null(captured.Data);
    }

    [Theory]
    [InlineData("Reading")]
    [InlineData("Paused")]
    [InlineData("Abandoned")]
    [InlineData("NotStarted")]
    public async Task Handle_NonFinishedStatus_StagesNothing(string newStatus)
    {
        var evt = new ReadingStatusChangedIntegrationEvent(
            _userId, _bookId, OldStatus: "Reading", NewStatus: newStatus, ChangedAt: DateTime.UtcNow, WorkId: Guid.NewGuid());

        await _handler.Handle(evt, CancellationToken.None);

        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
