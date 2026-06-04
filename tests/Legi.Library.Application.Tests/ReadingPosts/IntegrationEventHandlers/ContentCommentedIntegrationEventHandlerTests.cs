using Legi.Contracts.Social;
using Legi.Library.Application.ReadingPosts.IntegrationEventHandlers;
using Legi.Library.Domain.Entities;
using Legi.Library.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Library.Application.Tests.ReadingPosts.IntegrationEventHandlers;

public class ContentCommentedIntegrationEventHandlerTests
{
    private readonly Mock<IReadingPostRepository> _repo = new();
    private readonly ContentCommentedIntegrationEventHandler _handler;

    public ContentCommentedIntegrationEventHandlerTests()
    {
        _handler = new ContentCommentedIntegrationEventHandler(
            _repo.Object, NullLogger<ContentCommentedIntegrationEventHandler>.Instance);
    }

    private static ReadingProgress NewPost() =>
        ReadingProgress.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "content", null);

    [Fact]
    public async Task Handle_Post_IncrementsCommentsByExactlyOne()
    {
        var post = NewPost();
        _repo.Setup(r => r.GetByIdAsync(post.Id, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        await _handler.Handle(
            new ContentCommentedIntegrationEvent("Post", post.Id, Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(1, post.CommentsCount);
    }

    [Fact]
    public async Task Handle_ListTargetType_NoLoad_NoThrow()
    {
        await _handler.Handle(
            new ContentCommentedIntegrationEvent("List", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        _repo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_PostNotFound_TerminalNoOp_NoThrow()
    {
        _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadingProgress?)null);

        var ex = await Record.ExceptionAsync(() => _handler.Handle(
            new ContentCommentedIntegrationEvent("Post", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None));

        Assert.Null(ex);
    }
}
