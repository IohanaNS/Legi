using Legi.Contracts.Social;
using Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.IntegrationEventHandlers;

public class CommentDeletedIntegrationEventHandlerTests
{
    private readonly Mock<IReadingPostRepository> _repo = new();
    private readonly CommentDeletedIntegrationEventHandler _handler;

    public CommentDeletedIntegrationEventHandlerTests()
    {
        _handler = new CommentDeletedIntegrationEventHandler(
            _repo.Object, NullLogger<CommentDeletedIntegrationEventHandler>.Instance);
    }

    private static ReadingProgress NewPost() => ReadingProgressBuilder.Valid().Build();

    [Fact]
    public async Task Handle_Post_DecrementsCommentsByExactlyOne()
    {
        var post = NewPost();
        post.IncrementComments();
        post.IncrementComments(); // CommentsCount = 2
        _repo.Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _handler.Handle(
            SocialIntegrationEventFactory.CommentDeleted(targetId: post.Id),
            CancellationToken.None);

        Assert.Equal(1, post.CommentsCount);
    }

    [Fact]
    public async Task Handle_PostAlreadyAtZero_StaysZero_Floor()
    {
        var post = NewPost(); // CommentsCount = 0
        _repo.Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _handler.Handle(
            SocialIntegrationEventFactory.CommentDeleted(targetId: post.Id),
            CancellationToken.None);

        Assert.Equal(0, post.CommentsCount);
    }

    [Fact]
    public async Task Handle_ListTargetType_NoLoad_NoThrow()
    {
        await _handler.Handle(
            SocialIntegrationEventFactory.CommentDeleted(targetType: "List"),
            CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
