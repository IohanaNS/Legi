using Legi.Contracts.Social;
using Legi.Social.Application.Comments.EventHandlers;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Comments.EventHandlers;

public class CommentDeletedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBus = new();
    private readonly CommentDeletedDomainEventHandler _handler;

    public CommentDeletedDomainEventHandlerTests()
    {
        _handler = new CommentDeletedDomainEventHandler(
            _eventBus.Object, NullLogger<CommentDeletedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesCommentDeletedIntegrationEvent_WithStringifiedTargetType()
    {
        var commentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        // CommentDeletedDomainEvent's first param is the comment id (property: Id)
        var domainEvent = new CommentDeletedDomainEvent(
            commentId, userId, InteractableType.List, targetId);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _eventBus.Verify(
            x => x.PublishAsync(
                It.Is<CommentDeletedIntegrationEvent>(e =>
                    e.TargetType == "List" &&
                    e.TargetId == targetId &&
                    e.CommentId == commentId &&
                    e.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _eventBus.VerifyNoOtherCalls();
    }
}
