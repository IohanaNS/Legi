using Legi.Contracts.Library;
using Legi.Social.Application.Lists.IntegrationEventHandlers;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Lists.IntegrationEventHandlers;

public class UserListUpdatedIntegrationEventHandlerTests
{
    private readonly Mock<IUserProfileRepository> _userProfileRepository = new();
    private readonly Mock<IContentSnapshotRepository> _contentSnapshotRepository = new();
    private readonly UserListUpdatedIntegrationEventHandler _handler;

    public UserListUpdatedIntegrationEventHandlerTests()
    {
        _handler = new UserListUpdatedIntegrationEventHandler(
            _userProfileRepository.Object,
            _contentSnapshotRepository.Object,
            NullLogger<UserListUpdatedIntegrationEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublicList_StagesSnapshot()
    {
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        _userProfileRepository
            .Setup(r => r.GetByUserIdAsync(ownerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfileFactory.Create(ownerId));

        await _handler.Handle(
            new UserListUpdatedIntegrationEvent(listId, ownerId, "Sci-Fi", IsPublic: true),
            CancellationToken.None);

        _contentSnapshotRepository.Verify(
            r => r.StageAddOrUpdateAsync(
                It.Is<ContentSnapshot>(s => s.TargetType == InteractableType.List && s.TargetId == listId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _contentSnapshotRepository.Verify(
            r => r.StageDeleteByTargetAsync(It.IsAny<InteractableType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PrivateList_DeletesSnapshot()
    {
        var ownerId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        await _handler.Handle(
            new UserListUpdatedIntegrationEvent(listId, ownerId, "Sci-Fi", IsPublic: false),
            CancellationToken.None);

        _contentSnapshotRepository.Verify(
            r => r.StageDeleteByTargetAsync(InteractableType.List, listId, It.IsAny<CancellationToken>()),
            Times.Once);
        _contentSnapshotRepository.Verify(
            r => r.StageAddOrUpdateAsync(It.IsAny<ContentSnapshot>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
