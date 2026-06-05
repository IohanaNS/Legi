using Legi.Contracts.Social;
using Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;
using Legi.Library.Application.Tests.Factories;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.IntegrationEventHandlers;

public class ContentLikedIntegrationEventHandlerTests
{
    private readonly Mock<IReadingPostRepository> _repo = new();
    private readonly ContentLikedIntegrationEventHandler _handler;

    public ContentLikedIntegrationEventHandlerTests()
    {
        _handler = new ContentLikedIntegrationEventHandler(
            _repo.Object, NullLogger<ContentLikedIntegrationEventHandler>.Instance);
    }

    private static ReadingProgress NewPost() => ReadingProgressBuilder.Valid().Build();

    [Fact]
    public async Task Handle_Post_IncrementsLikesByExactlyOne()
    {
        var post = NewPost();
        var postId = post.Id;
        _repo.Setup(r => r.GetByIdAsync(postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _handler.Handle(
            SocialIntegrationEventFactory.ContentLiked(targetId: postId), CancellationToken.None);

        Assert.Equal(1, post.LikesCount);
    }

    [Fact]
    public async Task Handle_ListTargetType_NoLoad_NoMutation_NoThrow()
    {
        await _handler.Handle(
            SocialIntegrationEventFactory.ContentLiked(targetType: "List"), CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PostNotFound_TerminalNoOp_NoThrow()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadingProgress?)null);

        // Must not throw — a missing post is permanent (deleted), not a transient race.
        var ex = await Record.ExceptionAsync(() => _handler.Handle(
            SocialIntegrationEventFactory.ContentLiked(targetId: Guid.NewGuid()), CancellationToken.None));

        Assert.Null(ex);
    }
}
