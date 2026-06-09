using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Feed.Commands.DeleteFeedItem;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Feed.Commands;

public class DeleteFeedItemCommandHandlerTests
{
    private readonly Mock<IFeedItemRepository> _feedItems = new();
    private readonly DeleteFeedItemCommandHandler _handler;

    private readonly Guid _actorId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();

    public DeleteFeedItemCommandHandlerTests()
    {
        _handler = new DeleteFeedItemCommandHandler(_feedItems.Object);
    }

    [Fact]
    public async Task Handle_OwnAutomaticFeedItem_DeletesItem()
    {
        var feedItem = CreateFeedItem(ActivityType.BookFinished, targetType: null);
        _feedItems.Setup(r => r.GetByIdAsync(feedItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedItem);

        await _handler.Handle(new DeleteFeedItemCommand(_actorId, feedItem.Id), CancellationToken.None);

        _feedItems.Verify(r => r.DeleteAsync(feedItem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_MissingFeedItem_ThrowsNotFound()
    {
        var feedItemId = Guid.NewGuid();
        _feedItems.Setup(r => r.GetByIdAsync(feedItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeedItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new DeleteFeedItemCommand(_actorId, feedItemId), CancellationToken.None));
        _feedItems.Verify(r => r.DeleteAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DifferentActor_ThrowsForbidden()
    {
        var feedItem = CreateFeedItem(ActivityType.BookAdded, targetType: null);
        _feedItems.Setup(r => r.GetByIdAsync(feedItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedItem);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => _handler.Handle(new DeleteFeedItemCommand(Guid.NewGuid(), feedItem.Id), CancellationToken.None));
        _feedItems.Verify(r => r.DeleteAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ContentBackedFeedItem_ThrowsConflict()
    {
        var feedItem = CreateFeedItem(ActivityType.ProgressPosted, InteractableType.Post);
        _feedItems.Setup(r => r.GetByIdAsync(feedItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(feedItem);

        await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(new DeleteFeedItemCommand(_actorId, feedItem.Id), CancellationToken.None));
        _feedItems.Verify(r => r.DeleteAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private FeedItem CreateFeedItem(ActivityType activityType, InteractableType? targetType)
    {
        return FeedItem.Create(
            actorId: _actorId,
            actorUsername: "reader",
            actorAvatarUrl: null,
            activityType: activityType,
            targetType: targetType,
            referenceId: targetType is null ? _bookId : Guid.NewGuid(),
            bookTitle: "O Nome do Vento",
            bookAuthor: "Patrick Rothfuss",
            bookCoverUrl: null,
            data: null,
            bookId: _bookId);
    }
}
