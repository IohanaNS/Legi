using Legi.Social.Application.Comments.Commands.DeleteComment;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Comments.Commands;

public class DeleteCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _commentRepository = new();
    private readonly Mock<IContentSnapshotRepository> _contentSnapshotRepository = new();
    private readonly DeleteCommentCommandHandler _handler;

    public DeleteCommentCommandHandlerTests()
    {
        _handler = new DeleteCommentCommandHandler(
            _commentRepository.Object,
            _contentSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_ActorIsCommentAuthor_MarksForDeletionAndDeletes()
    {
        var comment = CommentFactory.Create();
        comment.ClearDomainEvents();
        var command = new DeleteCommentCommand(comment.UserId, comment.Id);

        _commentRepository
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);

        await _handler.Handle(command, CancellationToken.None);

        var domainEvent = Assert.IsType<CommentDeletedDomainEvent>(
            Assert.Single(comment.DomainEvents));
        Assert.Equal(comment.Id, domainEvent.Id);
        Assert.Equal(comment.UserId, domainEvent.UserId);

        _contentSnapshotRepository.Verify(
            r => r.GetByTargetAsync(It.IsAny<InteractableType>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _commentRepository.Verify(
            r => r.DeleteAsync(comment, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ActorIsContentOwner_MarksForDeletionAndDeletes()
    {
        var actorId = Guid.NewGuid();
        var comment = CommentFactory.Create();
        comment.ClearDomainEvents();
        var snapshot = ContentSnapshotFactory.Create(
            targetType: comment.TargetType,
            targetId: comment.TargetId,
            ownerId: actorId);

        _commentRepository
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(comment.TargetType, comment.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        await _handler.Handle(new DeleteCommentCommand(actorId, comment.Id), CancellationToken.None);

        Assert.IsType<CommentDeletedDomainEvent>(Assert.Single(comment.DomainEvents));
        _commentRepository.Verify(
            r => r.DeleteAsync(comment, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ActorIsNotAuthorOrContentOwner_ThrowsForbiddenException()
    {
        var actorId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var comment = CommentFactory.Create();
        comment.ClearDomainEvents();
        var snapshot = ContentSnapshotFactory.Create(
            targetType: comment.TargetType,
            targetId: comment.TargetId,
            ownerId: ownerId);

        _commentRepository
            .Setup(r => r.GetByIdAsync(comment.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(comment);
        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(comment.TargetType, comment.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _handler.Handle(new DeleteCommentCommand(actorId, comment.Id), CancellationToken.None));

        Assert.Empty(comment.DomainEvents);
        _commentRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CommentMissing_ThrowsNotFoundException()
    {
        var command = new DeleteCommentCommand(Guid.NewGuid(), Guid.NewGuid());
        _commentRepository
            .Setup(r => r.GetByIdAsync(command.CommentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _commentRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
