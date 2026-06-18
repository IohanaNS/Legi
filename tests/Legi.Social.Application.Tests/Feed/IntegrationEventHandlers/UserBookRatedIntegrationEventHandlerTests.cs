using System.Text.Json;
using Legi.Contracts.Library;
using Legi.SharedKernel;
using Legi.Social.Application.Feed.IntegrationEventHandlers;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Legi.Social.Application.Tests.Feed.IntegrationEventHandlers;

public class UserBookRatedIntegrationEventHandlerTests
{
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IBookSnapshotRepository> _books = new();
    private readonly Mock<IFeedItemRepository> _feed = new();
    private readonly UserBookRatedIntegrationEventHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _bookId = Guid.NewGuid();

    public UserBookRatedIntegrationEventHandlerTests()
    {
        _handler = new UserBookRatedIntegrationEventHandler(
            _profiles.Object, _books.Object, _feed.Object,
            NullLogger<UserBookRatedIntegrationEventHandler>.Instance);

        _profiles.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserProfile.Create(_userId, "dave"));
        _books.Setup(r => r.GetByBookIdAsync(_bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BookSnapshot.Create(_bookId, "Dune", "Frank Herbert", null, null));
    }

    [Fact]
    public async Task Handle_StagesBookRatedFeedItem_WithHalfStarConvertedToDisplayStars()
    {
        FeedItem? captured = null;
        _feed.Setup(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()))
            .Callback<FeedItem, CancellationToken>((fi, _) => captured = fi)
            .Returns(Task.CompletedTask);

        // half-star int 9 → 4.5 display stars
        var evt = new UserBookRatedIntegrationEvent(_bookId, _userId, Rating: 9, PreviousRating: 6, WorkId: Guid.NewGuid());

        await _handler.Handle(evt, CancellationToken.None);

        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(captured);
        Assert.Equal(ActivityType.BookRated, captured!.ActivityType);
        Assert.Null(captured.TargetType);
        Assert.Equal(_bookId, captured.ReferenceId);
        Assert.Equal("dave", captured.ActorUsername);

        using var doc = JsonDocument.Parse(captured.Data!);
        Assert.Equal(4.5, doc.RootElement.GetProperty("rating").GetDouble());
    }

    [Fact]
    public async Task Handle_MissingProfile_Throws()
    {
        _profiles.Setup(r => r.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        var evt = new UserBookRatedIntegrationEvent(_bookId, _userId, Rating: 8, PreviousRating: null, WorkId: Guid.NewGuid());

        await Assert.ThrowsAsync<TransientMessagingException>(
            () => _handler.Handle(evt, CancellationToken.None));
        _feed.Verify(r => r.StageAddAsync(It.IsAny<FeedItem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
