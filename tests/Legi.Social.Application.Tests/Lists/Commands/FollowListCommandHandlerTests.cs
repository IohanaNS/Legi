using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Lists.Commands.FollowList;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Lists.Commands;

public class FollowListCommandHandlerTests
{
    private readonly Mock<IContentSnapshotRepository> _contentSnapshotRepository = new();
    private readonly Mock<IListFollowRepository> _listFollowRepository = new();
    private readonly FollowListCommandHandler _handler;

    public FollowListCommandHandlerTests()
    {
        _handler = new FollowListCommandHandler(
            _contentSnapshotRepository.Object,
            _listFollowRepository.Object);
    }

    [Fact]
    public async Task Handle_PublicListAndNotAlreadyFollowing_AddsFollow()
    {
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        ListFollow? added = null;

        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(InteractableType.List, listId, ownerId: Guid.NewGuid()));
        _listFollowRepository
            .Setup(r => r.ExistsAsync(userId, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _listFollowRepository
            .Setup(r => r.AddAsync(It.IsAny<ListFollow>(), It.IsAny<CancellationToken>()))
            .Callback<ListFollow, CancellationToken>((f, _) => added = f)
            .Returns(Task.CompletedTask);

        await _handler.Handle(new FollowListCommand(userId, listId), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(userId, added!.UserId);
        Assert.Equal(listId, added.ListId);
    }

    [Fact]
    public async Task Handle_PrivateList_ThrowsNotFound()
    {
        var listId = Guid.NewGuid();
        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentSnapshot?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new FollowListCommand(Guid.NewGuid(), listId), CancellationToken.None));

        _listFollowRepository.Verify(
            r => r.AddAsync(It.IsAny<ListFollow>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OwnerFollowsOwnList_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(InteractableType.List, listId, ownerId: ownerId));

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new FollowListCommand(ownerId, listId), CancellationToken.None));

        _listFollowRepository.Verify(
            r => r.AddAsync(It.IsAny<ListFollow>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyFollowing_ThrowsConflict()
    {
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(InteractableType.List, listId, ownerId: Guid.NewGuid()));
        _listFollowRepository
            .Setup(r => r.ExistsAsync(userId, listId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new FollowListCommand(userId, listId), CancellationToken.None));

        _listFollowRepository.Verify(
            r => r.AddAsync(It.IsAny<ListFollow>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
