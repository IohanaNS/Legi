using Legi.Contracts.Social;
using Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.IntegrationEventHandlers;

public class ContentUnlikedIntegrationEventHandlerTests
{
    private readonly Mock<IReadingPostRepository> _repo = new();
    private readonly ContentUnlikedIntegrationEventHandler _handler;

    public ContentUnlikedIntegrationEventHandlerTests()
    {
        _handler = new ContentUnlikedIntegrationEventHandler(
            _repo.Object, NullLogger<ContentUnlikedIntegrationEventHandler>.Instance);
    }

    private static ReadingProgress NewPost() =>
        ReadingProgress.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "content", null);

    [Fact]
    public async Task Handle_Post_DecrementsLikesByExactlyOne()
    {
        var post = NewPost();
        post.IncrementLikes();
        post.IncrementLikes(); // LikesCount = 2
        _repo.Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _handler.Handle(
            new ContentUnlikedIntegrationEvent("Post", post.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(1, post.LikesCount);
    }

    [Fact]
    public async Task Handle_PostAlreadyAtZero_StaysZero_Floor()
    {
        var post = NewPost(); // LikesCount = 0
        _repo.Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _handler.Handle(
            new ContentUnlikedIntegrationEvent("Post", post.Id, Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, post.LikesCount);
    }

    [Fact]
    public async Task Handle_ListTargetType_NoLoad_NoThrow()
    {
        await _handler.Handle(
            new ContentUnlikedIntegrationEvent("List", Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
