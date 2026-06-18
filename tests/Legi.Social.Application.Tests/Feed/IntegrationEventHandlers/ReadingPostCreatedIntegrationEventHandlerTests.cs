using System.Text.Json;
using Legi.Contracts.Library;
using Legi.SharedKernel;
using Legi.Social.Application.Feed.IntegrationEventHandlers;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Feed.IntegrationEventHandlers;

public class ReadingPostCreatedIntegrationEventHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IBookSnapshotRepository> _books = new();
    private readonly Mock<IContentSnapshotRepository> _content = new();
    private readonly Mock<IFeedItemRepository> _feed = new();
    private readonly ReadingPostCreatedIntegrationEventHandler _handler;

    private readonly Guid _postId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();

    public ReadingPostCreatedIntegrationEventHandlerTests()
    {
        _handler = new ReadingPostCreatedIntegrationEventHandler(
            _profiles.Object, _books.Object, _content.Object, _feed.Object,
            NullLogger<ReadingPostCreatedIntegrationEventHandler>.Instance);

        var profile = UserProfile.Create(_userId, "carol");
        profile.UpdateAvatar("https://cdn/carol.png");
        _profiles.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        _books.Setup(r => r.GetByBookIdAsync(_bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshot.Create(_bookId, "Dune", "Frank Herbert", "https://cdn/dune.png", 412));
    }

    [Fact]
    public async Task Handle_StagesContentSnapshotAndProgressPostedFeedItem_WithResolvedData()
    {
        ContentSnapshot? snapshot = null;
        FeedItem? feedItem = null;
        _content.Setup(r => r.StageAddOrUpdateAsync(It.IsAny<ContentSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<ContentSnapshot, CancellationToken>((cs, _) => snapshot = cs)
            .Returns(Task.CompletedTask);
        _feed.Setup(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((fi, _) => feedItem = fi)
            .Returns(Task.CompletedTask);

        var evt = new ReadingPostCreatedIntegrationEvent(
            _postId, _userId, _bookId,
            Content: "Halfway through, love it",
            ProgressValue: 50, ProgressType: "Percentage",
            CreatedAt: DateTime.UtcNow,
            WorkId: Guid.NewGuid());

        await _handler.Handle(evt, CancellationToken.None);

        // ContentSnapshot: owner (for delete authz) + book/preview context
        Assert.NotNull(snapshot);
        Assert.Equal(InteractableType.Post, snapshot!.TargetType);
        Assert.Equal(_postId, snapshot.TargetId);
        Assert.Equal(_userId, snapshot.OwnerId);
        Assert.Equal("carol", snapshot.OwnerUsername);
        Assert.Equal("https://cdn/carol.png", snapshot.OwnerAvatarUrl);
        Assert.Equal("Dune", snapshot.BookTitle);
        Assert.Equal("Frank Herbert", snapshot.BookAuthor);
        Assert.Equal("Halfway through, love it", snapshot.ContentPreview);

        // FeedItem: ProgressPosted, interactable (TargetType=Post, ReferenceId=PostId)
        Assert.NotNull(feedItem);
        Assert.Equal(ActivityType.ProgressPosted, feedItem!.ActivityType);
        Assert.Equal(InteractableType.Post, feedItem.TargetType);
        Assert.Equal(_postId, feedItem.ReferenceId);
        Assert.Equal("carol", feedItem.ActorUsername);
        Assert.Equal("Dune", feedItem.BookTitle);

        using var doc = JsonDocument.Parse(feedItem.Data!);
        var root = doc.RootElement;
        Assert.Equal(50, root.GetProperty("progress").GetInt32());
        Assert.Equal("Percentage", root.GetProperty("progressType").GetString());
        Assert.Equal("Halfway through, love it", root.GetProperty("content").GetString());
    }

    [Fact]
    public async Task Handle_ContentOnlyPost_OmitsNullProgressKeysFromData()
    {
        FeedItem? feedItem = null;
        _feed.Setup(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((fi, _) => feedItem = fi)
            .Returns(Task.CompletedTask);

        var evt = new ReadingPostCreatedIntegrationEvent(
            _postId, _userId, _bookId,
            Content: "Just a thought, no progress yet",
            ProgressValue: null, ProgressType: null,
            CreatedAt: DateTime.UtcNow,
            WorkId: Guid.NewGuid());

        await _handler.Handle(evt, CancellationToken.None);

        using var doc = JsonDocument.Parse(feedItem!.Data!);
        var root = doc.RootElement;
        Assert.False(root.TryGetProperty("progress", out _));
        Assert.False(root.TryGetProperty("progressType", out _));
        Assert.Equal("Just a thought, no progress yet", root.GetProperty("content").GetString());
        Assert.False(root.TryGetProperty("isSpoiler", out _));
    }

    [Fact]
    public async Task Handle_SpoilerPost_AddsSpoilerFlagAndOmitsContentPreview()
    {
        ContentSnapshot? snapshot = null;
        FeedItem? feedItem = null;
        _content.Setup(r => r.StageAddOrUpdateAsync(It.IsAny<ContentSnapshot>(), It.IsAny<CancellationToken>()))
            .Callback<ContentSnapshot, CancellationToken>((cs, _) => snapshot = cs)
            .Returns(Task.CompletedTask);
        _feed.Setup(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((fi, _) => feedItem = fi)
            .Returns(Task.CompletedTask);

        var evt = new ReadingPostCreatedIntegrationEvent(
            _postId, _userId, _bookId,
            Content: "The ending changes everything",
            ProgressValue: 80, ProgressType: "Percentage",
            CreatedAt: DateTime.UtcNow,
            WorkId: Guid.NewGuid(),
            IsSpoiler: true);

        await _handler.Handle(evt, CancellationToken.None);

        Assert.NotNull(snapshot);
        Assert.Null(snapshot!.ContentPreview);

        using var doc = JsonDocument.Parse(feedItem!.Data!);
        var root = doc.RootElement;
        Assert.Equal("The ending changes everything", root.GetProperty("content").GetString());
        Assert.True(root.GetProperty("isSpoiler").GetBoolean());
    }

    [Fact]
    public async Task Handle_MissingBookSnapshot_Throws_AndStagesNothing()
    {
        _books.Setup(r => r.GetByBookIdAsync(_bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BookSnapshot?)null);

        var evt = new ReadingPostCreatedIntegrationEvent(
            _postId, _userId, _bookId, "hi", 10, "Page", DateTime.UtcNow, WorkId: Guid.NewGuid());

        await Assert.ThrowsAsync<TransientMessagingException>(
            () => _handler.Handle(evt, CancellationToken.None));

        _content.Verify(r => r.StageAddOrUpdateAsync(It.IsAny<ContentSnapshot>(), It.IsAny<CancellationToken>()), Times.Never);
        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
