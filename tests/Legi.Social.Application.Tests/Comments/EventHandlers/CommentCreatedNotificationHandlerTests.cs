using Legi.Social.Application.Comments.EventHandlers;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Comments.EventHandlers;

public class CommentCreatedNotificationHandlerTests
{
    private readonly Mock<IContentSnapshotRepository> _contentSnapshots = new();
    private readonly Mock<IUserProfileRepository> _userProfiles = new();
    private readonly Mock<INotificationRepository> _notifications = new();
    private readonly CommentCreatedNotificationHandler _handler;

    public CommentCreatedNotificationHandlerTests()
    {
        _handler = new CommentCreatedNotificationHandler(
            _contentSnapshots.Object,
            _userProfiles.Object,
            _notifications.Object,
            NullLogger<CommentCreatedNotificationHandler>.Instance);
    }

    [Fact]
    public async Task Handle_StagesCommentNotification_WithPreview_WhenActorIsNotOwner()
    {
        var ownerId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var commentId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _contentSnapshots
            .Setup(r => r.GetByTargetAsync(InteractableType.Post, targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(targetId: targetId, ownerId: ownerId));
        _userProfiles
            .Setup(r => r.GetByUserIdAsync(actorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfileFactory.Create(actorId, "carlos"));

        var domainEvent = new CommentCreatedDomainEvent(
            commentId, actorId, InteractableType.Post, targetId, "Loved this!");

        await _handler.Handle(domainEvent, CancellationToken.None);

        _notifications.Verify(r => r.StageAdd(It.Is<Notification>(n =>
            n.RecipientId == ownerId &&
            n.ActorId == actorId &&
            n.NotificationType == NotificationType.Comment &&
            n.TargetType == InteractableType.Post &&
            n.TargetId == targetId &&
            n.CommentPreview == "Loved this!")), Times.Once);
    }

    [Fact]
    public async Task Handle_DoesNotCreateNotification_WhenActorIsOwner()
    {
        var ownerId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        _contentSnapshots
            .Setup(r => r.GetByTargetAsync(InteractableType.Post, targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(targetId: targetId, ownerId: ownerId));

        var domainEvent = new CommentCreatedDomainEvent(
            Guid.NewGuid(), ownerId, InteractableType.Post, targetId, "self comment");

        await _handler.Handle(domainEvent, CancellationToken.None);

        _notifications.Verify(r => r.StageAdd(It.IsAny<Notification>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ReturnsEarly_WhenSnapshotMissing()
    {
        var targetId = Guid.NewGuid();
        _contentSnapshots
            .Setup(r => r.GetByTargetAsync(It.IsAny<InteractableType>(), targetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentSnapshot?)null);

        var domainEvent = new CommentCreatedDomainEvent(
            Guid.NewGuid(), Guid.NewGuid(), InteractableType.Post, targetId, "hi");

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

        var domainEvent = new CommentCreatedDomainEvent(
            Guid.NewGuid(), actorId, InteractableType.Post, targetId, "hi");

        await _handler.Handle(domainEvent, CancellationToken.None);

        _notifications.Verify(r => r.StageAdd(It.IsAny<Notification>()), Times.Never);
    }
}
