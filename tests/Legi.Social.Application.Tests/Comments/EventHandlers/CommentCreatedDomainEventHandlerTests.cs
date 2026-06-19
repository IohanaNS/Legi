using Legi.Contracts.Social;
using Legi.Social.Application.Comments.EventHandlers;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Comments.EventHandlers;

public class CommentCreatedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBus = new();
    private readonly CommentCreatedDomainEventHandler _handler;

    public CommentCreatedDomainEventHandlerTests()
    {
        _handler = new CommentCreatedDomainEventHandler(
            _eventBus.Object, NullLogger<CommentCreatedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesContentCommentedIntegrationEvent_WithStringifiedTargetType()
    {
        var commentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var domainEvent = new CommentCreatedDomainEvent(
            commentId, userId, InteractableType.Post, targetId, "Nice post!");

        await _handler.Handle(domainEvent, CancellationToken.None);

        _eventBus.Verify(
            x => x.PublishAsync(
                It.Is<ContentCommentedIntegrationEvent>(e =>
                    e.TargetType == "Post" &&
                    e.TargetId == targetId &&
                    e.CommentId == commentId &&
                    e.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _eventBus.VerifyNoOtherCalls();
    }
}
