using Legi.Contracts.Library;
using Legi.Social.Application.Feed.IntegrationEventHandlers;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Feed.IntegrationEventHandlers;

public class ReadingPostDeletedIntegrationEventHandlerTests
{
    private readonly Mock<IContentSnapshotRepository> _content = new();
    private readonly Mock<ILikeRepository> _likes = new();
    private readonly Mock<ICommentRepository> _comments = new();
    private readonly Mock<IFeedItemRepository> _feed = new();
    private readonly ReadingPostDeletedIntegrationEventHandler _handler;

    public ReadingPostDeletedIntegrationEventHandlerTests()
    {
        _handler = new ReadingPostDeletedIntegrationEventHandler(
            _content.Object, _likes.Object, _comments.Object, _feed.Object,
            NullLogger<ReadingPostDeletedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PurgesAllFourTargets_ByPostId_AndStagesNoOutgoingWork()
    {
        var postId = Guid.NewGuid();
        var evt = new ReadingPostDeletedIntegrationEvent(postId, Guid.NewGuid());

        await _handler.Handle(evt, CancellationToken.None);

        _content.Verify(r => r.StageDeleteByTargetAsync(
            InteractableType.Post, postId, It.IsAny<CancellationToken>()), Times.Once);
        _likes.Verify(r => r.StageDeleteByTargetAsync(
            InteractableType.Post, postId, It.IsAny<CancellationToken>()), Times.Once);
        _comments.Verify(r => r.StageDeleteByTargetAsync(
            InteractableType.Post, postId, It.IsAny<CancellationToken>()), Times.Once);
        _feed.Verify(r => r.StageDeleteByReferenceAsync(
            postId, It.IsAny<CancellationToken>()), Times.Once);

        // Pure cascade: no creation, no re-emitted domain events (decision 8.1.2).
        _content.VerifyNoOtherCalls();
        _likes.VerifyNoOtherCalls();
        _comments.VerifyNoOtherCalls();
        _feed.VerifyNoOtherCalls();
    }
}
