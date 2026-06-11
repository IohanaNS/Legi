using Legi.Contracts.Library;
using Legi.Social.Application.Lists.IntegrationEventHandlers;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Lists.IntegrationEventHandlers;

public class UserListDeletedIntegrationEventHandlerTests
{
    private readonly Mock<IContentSnapshotRepository> _contentSnapshotRepository = new();
    private readonly Mock<ILikeRepository> _likeRepository = new();
    private readonly Mock<ICommentRepository> _commentRepository = new();
    private readonly Mock<IListFollowRepository> _listFollowRepository = new();
    private readonly UserListDeletedIntegrationEventHandler _handler;

    public UserListDeletedIntegrationEventHandlerTests()
    {
        _handler = new UserListDeletedIntegrationEventHandler(
            _contentSnapshotRepository.Object,
            _likeRepository.Object,
            _commentRepository.Object,
            _listFollowRepository.Object,
            NullLogger<UserListDeletedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PurgesSnapshotLikesCommentsAndFollows()
    {
        var listId = Guid.NewGuid();

        await _handler.Handle(
            new UserListDeletedIntegrationEvent(listId, Guid.NewGuid()),
            CancellationToken.None);

        _contentSnapshotRepository.Verify(
            r => r.StageDeleteByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()), Times.Once);
        _likeRepository.Verify(
            r => r.StageDeleteByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()), Times.Once);
        _commentRepository.Verify(
            r => r.StageDeleteByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()), Times.Once);
        _listFollowRepository.Verify(
            r => r.StageDeleteByListAsync(listId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
