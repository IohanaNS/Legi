using Legi.Social.Application.Likes.EventHandlers;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Likes.EventHandlers;

public class ContentLikedNotificationHandlerTests
{
    private readonly Mock<IContentSnapshotRepository> _contentSnapshots = new();
    private readonly Mock<IUserProfileRepository> _userProfiles = new();
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly ContentLikedNotificationHandler _handler;

    public ContentLikedNotificationHandlerTests()
    {
        _handler = new ContentLikedNotificationHandler(
            _contentSnapshots.Object,
            _userProfiles.Object,
            _notifications.Object,
            NullLogger<ContentLikedNotificationHandler>.Instance);
    }

    [Fact]
    public async Task Handle_StagesNotification_WhenActorIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var snapshot = ContentSnapshotFactory.Create(
            targetType: InteractableType.Review, targetId: targetId, ownerId: ownerId);
        _contentSnapshots
            .Setup(r => r.GetByTargetAsync(InteractableType.Review, targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        _userProfiles
            .Setup(r => r.GetByUserIdAsync(actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfileFactory.Create(actorId, "carlos"));

        var domainEvent = new ContentLikedDomainEvent(actorId, InteractableType.Review, targetId);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _notifications.Verify(r => r.StageAdd(It.Is<Notification>(n =>
            n.RecipientId == ownerId &&
            n.ActorId == actorId &&
            n.NotificationType == NotificationType.Like &&
            n.TargetType == InteractableType.Review &&
            n.TargetId == targetId)), Times.Once);
    }

    [Fact]
    public async Task Handle_DoesNotCreateNotification_WhenActorIsOwner()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        var snapshot = ContentSnapshotFactory.Create(
            targetType: InteractableType.Post, targetId: targetId, ownerId: ownerId);
        _contentSnapshots
            .Setup(r => r.GetByTargetAsync(InteractableType.Post, targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        // Actor == owner.
        var domainEvent = new ContentLikedDomainEvent(ownerId, InteractableType.Post, targetId);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _notifications.Verify(r => r.StageAdd(It.IsAny<Notification>()), Times.Never);
        _userProfiles.Verify(
            r => r.GetByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsEarly_WhenSnapshotMissing()
    {
        var targetId = Guid.NewGuid();
        _contentSnapshots
            .Setup(r => r.GetByTargetAsync(It.IsAny<InteractableType>(), targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentSnapshot?)null);

        var domainEvent = new ContentLikedDomainEvent(Guid.NewGuid(), InteractableType.Post, targetId);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _notifications.Verify(r => r.StageAdd(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsEarly_WhenActorProfileMissing()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _contentSnapshots
            .Setup(r => r.GetByTargetAsync(InteractableType.Post, targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(targetId: targetId, ownerId: ownerId));
        _userProfiles
            .Setup(r => r.GetByUserIdAsync(actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var domainEvent = new ContentLikedDomainEvent(actorId, InteractableType.Post, targetId);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _notifications.Verify(r => r.StageAdd(It.IsAny<Notification>()), Times.Never);
    }
}
