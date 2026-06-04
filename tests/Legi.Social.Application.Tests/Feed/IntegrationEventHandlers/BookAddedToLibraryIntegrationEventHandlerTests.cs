using Legi.Contracts.Library;
using Legi.Social.Application.Feed.IntegrationEventHandlers;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Feed.IntegrationEventHandlers;

public class BookAddedToLibraryIntegrationEventHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IBookSnapshotRepository> _books = new();
    private readonly Mock<IFeedItemRepository> _feed = new();
    private readonly BookAddedToLibraryIntegrationEventHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();

    public BookAddedToLibraryIntegrationEventHandlerTests()
    {
        _handler = new BookAddedToLibraryIntegrationEventHandler(
            _profiles.Object, _books.Object, _feed.Object,
            NullLogger<BookAddedToLibraryIntegrationEventHandler>.Instance);
    }

    private void SeedProfileAndBook()
    {
        var profile = UserProfile.Create(_userId, "alice");
        profile.UpdateAvatar("https://cdn/alice.png");
        _profiles.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _books.Setup(r => r.GetByBookIdAsync(_bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshot.Create(_bookId, "Dune", "Frank Herbert", "https://cdn/dune.png", 412));
    }

    private FeedItem CaptureStagedFeedItem()
    {
        FeedItem? captured = null;
        _feed.Setup(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((fi, _) => captured = fi)
            .Returns(Task.CompletedTask);
        return captured!;
    }

    [Fact]
    public async Task Handle_RealAdd_StagesBookStartedFeedItem_WithResolvedActorAndBook()
    {
        SeedProfileAndBook();
        FeedItem? captured = null;
        _feed.Setup(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((fi, _) => captured = fi)
            .Returns(Task.CompletedTask);

        var evt = new BookAddedToLibraryIntegrationEvent(
            Guid.NewGuid(), _userId, _bookId, Wishlist: false, AddedAt: DateTime.UtcNow);

        await _handler.Handle(evt, CancellationToken.None);

        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(ActivityType.BookStarted, captured!.ActivityType);
        Assert.Null(captured.TargetType);
        Assert.Equal(_userId, captured.ActorId);
        Assert.Equal("alice", captured.ActorUsername);
        Assert.Equal("https://cdn/alice.png", captured.ActorAvatarUrl);
        Assert.Equal(_bookId, captured.ReferenceId);
        Assert.Equal("Dune", captured.BookTitle);
        Assert.Equal("Frank Herbert", captured.BookAuthor);
        Assert.Equal("https://cdn/dune.png", captured.BookCoverUrl);
        Assert.Null(captured.Data);
    }

    [Fact]
    public async Task Handle_WishlistAdd_StagesNothing_AndSkipsLookups()
    {
        var evt = new BookAddedToLibraryIntegrationEvent(
            Guid.NewGuid(), _userId, _bookId, Wishlist: true, AddedAt: DateTime.UtcNow);

        await _handler.Handle(evt, CancellationToken.None);

        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _profiles.VerifyNoOtherCalls();
        _books.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_MissingProfile_Throws_SoBrokerRedelivers()
    {
        _profiles.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var evt = new BookAddedToLibraryIntegrationEvent(
            Guid.NewGuid(), _userId, _bookId, Wishlist: false, AddedAt: DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(evt, CancellationToken.None));
        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_MissingBookSnapshot_Throws_SoBrokerRedelivers()
    {
        _profiles.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(_userId, "alice"));
        _books.Setup(r => r.GetByBookIdAsync(_bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookSnapshot?)null);

        var evt = new BookAddedToLibraryIntegrationEvent(
            Guid.NewGuid(), _userId, _bookId, Wishlist: false, AddedAt: DateTime.UtcNow);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(evt, CancellationToken.None));
        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CalledTwice_StagesTwoItems_BecauseDedupIsTheInboxsJob()
    {
        // The handler is intentionally NOT self-idempotent: FeedItem has no natural
        // key (fresh Guid per Create). Duplicate suppression lives in the
        // IntegrationEventDispatcher's inbox check, not here (decision 8.1.1).
        SeedProfileAndBook();
        _ = CaptureStagedFeedItem();

        var evt = new BookAddedToLibraryIntegrationEvent(
            Guid.NewGuid(), _userId, _bookId, Wishlist: false, AddedAt: DateTime.UtcNow);

        await _handler.Handle(evt, CancellationToken.None);
        await _handler.Handle(evt, CancellationToken.None);

        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
