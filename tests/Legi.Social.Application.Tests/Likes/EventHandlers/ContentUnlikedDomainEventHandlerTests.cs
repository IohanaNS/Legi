using Legi.Contracts.Social;
using Legi.Social.Application.Likes.EventHandlers;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.SharedKernel;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Likes.EventHandlers;

public class ContentUnlikedDomainEventHandlerTests
{
    private readonly Mock<IEventBus> _eventBus = new();
    private readonly ContentUnlikedDomainEventHandler _handler;

    public ContentUnlikedDomainEventHandlerTests()
    {
        _handler = new ContentUnlikedDomainEventHandler(
            _eventBus.Object, NullLogger<ContentUnlikedDomainEventHandler>.Instance);
    }

    [Fact]
    public async Task Handle_PublishesContentUnlikedIntegrationEvent_WithStringifiedTargetType()
    {
        var userId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var domainEvent = new ContentUnlikedDomainEvent(userId, InteractableType.List, targetId);

        await _handler.Handle(domainEvent, CancellationToken.None);

        _eventBus.Verify(
            x => x.PublishAsync(
                It.Is<ContentUnlikedIntegrationEvent>(e =>
                    e.TargetType == "List" &&
                    e.TargetId == targetId &&
                    e.UserId == userId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _eventBus.VerifyNoOtherCalls();
    }
}
