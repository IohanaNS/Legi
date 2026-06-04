using Legi.Contracts.Social;
using Legi.Social.Application.Likes.EventHandlers;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Likes.EventHandlers;

public class ContentLikedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBus = new();
    private readonly ContentLikedDomainEventHandler _handler;

    public ContentLikedDomainEventHandlerTests()
    {
        _handler = new ContentLikedDomainEventHandler(
            _eventBus.Object, NullLogger<ContentLikedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesContentLikedIntegrationEvent_WithStringifiedTargetType()
    {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var domainEvent = new ContentLikedDomainEvent(userId, InteractableType.Post, targetId);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _eventBus.Verify(
            x => x.PublishAsync(
                It.Is<ContentLikedIntegrationEvent>(e =>
                    e.TargetType == "Post" &&
                    e.TargetId == targetId &&
                    e.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _eventBus.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_StringifiesListTargetType()
    {
        var domainEvent = new ContentLikedDomainEvent(
            Guid.NewGuid(), InteractableType.List, Guid.NewGuid());

        await _handler.Handle(domainEvent, CancellationToken.None);

        _eventBus.Verify(
            x => x.PublishAsync(
                It.Is<ContentLikedIntegrationEvent>(e => e.TargetType == "List"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
