using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class FeedItemTests
{
    [Fact]
    public void Create_ValidData_CreatesFeedItem()
    {
        var feedItem = FeedItem.Create(
            SocialTestIds.UserId,
            "reader",
            "https://cdn.example.com/avatar.png",
            ActivityType.ProgressPosted,
            InteractableType.Post,
            SocialTestIds.TargetId,
            "Dune",
            "Frank Herbert",
            "https://cdn.example.com/dune.png",
            """{"progress":50}""");

        Assert.NotEqual(Guid.Empty, feedItem.Id);
        Assert.Equal(SocialTestIds.UserId, feedItem.ActorId);
        Assert.Equal("reader", feedItem.ActorUsername);
        Assert.Equal("https://cdn.example.com/avatar.png", feedItem.ActorAvatarUrl);
        Assert.Equal(ActivityType.ProgressPosted, feedItem.ActivityType);
        Assert.Equal(InteractableType.Post, feedItem.TargetType);
        Assert.Equal(SocialTestIds.TargetId, feedItem.ReferenceId);
        Assert.Equal("Dune", feedItem.BookTitle);
        Assert.Equal("Frank Herbert", feedItem.BookAuthor);
        Assert.Equal("https://cdn.example.com/dune.png", feedItem.BookCoverUrl);
        Assert.Equal("""{"progress":50}""", feedItem.Data);
    }
}
