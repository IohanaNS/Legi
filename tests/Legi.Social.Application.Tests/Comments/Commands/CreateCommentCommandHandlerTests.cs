using Legi.Social.Application.Comments.Commands.CreateComment;
using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Comments.Commands;

public class CreateCommentCommandHandlerTests
{
    private readonly Mock<ICommentRepository> _commentRepository = new();
    private readonly Mock<IContentSnapshotRepository> _contentSnapshotRepository = new();
    private readonly CreateCommentCommandHandler _handler;

    public CreateCommentCommandHandlerTests()
    {
        _handler = new CreateCommentCommandHandler(
            _commentRepository.Object,
            _contentSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_ContentSnapshotExists_AddsComment()
    {
        var command = new CreateCommentCommand(
            Guid.NewGuid(),
            InteractableType.Post,
            Guid.NewGuid(),
            "This chapter was excellent");
        Comment? addedComment = null;

        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(command.TargetType, command.TargetId));
        _commentRepository
            .Setup(r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .Callback<Comment, CancellationToken>((comment, _) => addedComment = comment)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(addedComment);
        Assert.Equal(command.UserId, addedComment!.UserId);
        Assert.Equal(command.TargetType, addedComment.TargetType);
        Assert.Equal(command.TargetId, addedComment.TargetId);
        Assert.Equal(command.Content, addedComment.Content);
        Assert.Equal(addedComment.Id, response.CommentId);
        Assert.Equal(addedComment.CreatedAt, response.CreatedAt);

        var domainEvent = Assert.IsType<CommentCreatedDomainEvent>(
            Assert.Single(addedComment.DomainEvents));
        Assert.Equal(addedComment.Id, domainEvent.CommentId);
        Assert.Equal(command.UserId, domainEvent.UserId);
    }

    [Fact]
    public async Task Handle_ContentSnapshotMissing_ThrowsNotFoundException()
    {
        var command = new CreateCommentCommand(
            Guid.NewGuid(),
            InteractableType.Post,
            Guid.NewGuid(),
            "Nice update");
        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentSnapshot?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _commentRepository.Verify(
            r => r.AddAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
