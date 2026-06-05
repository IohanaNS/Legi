using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Likes.Commands.UnlikeContent;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Likes.Commands;

public class UnlikeContentCommandHandlerTests
{
    private readonly Mock<ILikeRepository> _likeRepository = new();
    private readonly UnlikeContentCommandHandler _handler;

    public UnlikeContentCommandHandlerTests()
    {
        _handler = new UnlikeContentCommandHandler(_likeRepository.Object);
    }

    [Fact]
    public async Task Handle_LikeExists_MarksForRemovalAndDeletes()
    {
        var like = LikeFactory.Create();
        like.ClearDomainEvents();
        var command = new UnlikeContentCommand(like.UserId, like.TargetType, like.TargetId);

        _likeRepository
            .Setup(r => r.GetByUserAndTargetAsync(command.UserId, command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(like);

        await _handler.Handle(command, CancellationToken.None);

        var domainEvent = Assert.IsType<ContentUnlikedDomainEvent>(
            Assert.Single(like.DomainEvents));
        Assert.Equal(command.UserId, domainEvent.UserId);
        Assert.Equal(command.TargetType, domainEvent.TargetType);
        Assert.Equal(command.TargetId, domainEvent.TargetId);

        _likeRepository.Verify(
            r => r.DeleteAsync(like, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LikeMissing_ThrowsNotFoundException()
    {
        var command = new UnlikeContentCommand(
            Guid.NewGuid(),
            InteractableType.Post,
            Guid.NewGuid());
        _likeRepository
            .Setup(r => r.GetByUserAndTargetAsync(command.UserId, command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Like?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _likeRepository.Verify(
            r => r.DeleteAsync(It.IsAny<Like>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
