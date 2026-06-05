using Legi.Social.Application.Common.Exceptions;
using Legi.Social.Application.Likes.Commands.LikeContent;
using Legi.Social.Application.Tests.Factories;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Repositories;
using Moq;

namespace Legi.Social.Application.Tests.Likes.Commands;

public class LikeContentCommandHandlerTests
{
    private readonly Mock<ILikeRepository> _likeRepository = new();
    private readonly Mock<IContentSnapshotRepository> _contentSnapshotRepository = new();
    private readonly LikeContentCommandHandler _handler;

    public LikeContentCommandHandlerTests()
    {
        _handler = new LikeContentCommandHandler(
            _likeRepository.Object,
            _contentSnapshotRepository.Object);
    }

    [Fact]
    public async Task Handle_ContentSnapshotExistsAndNotAlreadyLiked_AddsLike()
    {
        var command = new LikeContentCommand(
            Guid.NewGuid(),
            InteractableType.Post,
            Guid.NewGuid());
        Like? addedLike = null;

        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(command.TargetType, command.TargetId));
        _likeRepository
            .Setup(r => r.GetByUserAndTargetAsync(command.UserId, command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Like?)null);
        _likeRepository
            .Setup(r => r.AddAsync(It.IsAny<Like>(), It.IsAny<CancellationToken>()))
            .Callback<Like, CancellationToken>((like, _) => addedLike = like)
            .Returns(Task.CompletedTask);

        var response = await _handler.Handle(command, CancellationToken.None);

        Assert.NotNull(addedLike);
        Assert.Equal(command.UserId, addedLike!.UserId);
        Assert.Equal(command.TargetType, addedLike.TargetType);
        Assert.Equal(command.TargetId, addedLike.TargetId);
        Assert.Equal(addedLike.Id, response.LikeId);
        Assert.Equal(addedLike.CreatedAt, response.CreatedAt);

        var domainEvent = Assert.IsType<ContentLikedDomainEvent>(
            Assert.Single(addedLike.DomainEvents));
        Assert.Equal(command.UserId, domainEvent.UserId);
        Assert.Equal(command.TargetType, domainEvent.TargetType);
        Assert.Equal(command.TargetId, domainEvent.TargetId);
    }

    [Fact]
    public async Task Handle_ContentSnapshotMissing_ThrowsNotFoundException()
    {
        var command = new LikeContentCommand(
            Guid.NewGuid(),
            InteractableType.Post,
            Guid.NewGuid());
        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentSnapshot?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _likeRepository.Verify(
            r => r.GetByUserAndTargetAsync(
                It.IsAny<Guid>(),
                It.IsAny<InteractableType>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _likeRepository.Verify(
            r => r.AddAsync(It.IsAny<Like>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ContentAlreadyLiked_ThrowsConflictException()
    {
        var command = new LikeContentCommand(
            Guid.NewGuid(),
            InteractableType.Post,
            Guid.NewGuid());
        var existingLike = LikeFactory.Create(
            command.UserId,
            command.TargetType,
            command.TargetId);

        _contentSnapshotRepository
            .Setup(r => r.GetByTargetAsync(command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContentSnapshotFactory.Create(command.TargetType, command.TargetId));
        _likeRepository
            .Setup(r => r.GetByUserAndTargetAsync(command.UserId, command.TargetType, command.TargetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLike);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(command, CancellationToken.None));

        _likeRepository.Verify(
            r => r.AddAsync(It.IsAny<Like>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
