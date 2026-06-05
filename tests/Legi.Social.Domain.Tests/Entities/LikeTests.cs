using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class LikeTests
{
    [Fact]
    public void Create_ValidData_CreatesLikeAndRaisesLikedEvent()
    {
        var like = Like.Create(
            SocialTestIds.UserId,
            InteractableType.Post,
            SocialTestIds.TargetId);

        Assert.NotEqual(Guid.Empty, like.Id);
        Assert.Equal(SocialTestIds.UserId, like.UserId);
        Assert.Equal(InteractableType.Post, like.TargetType);
        Assert.Equal(SocialTestIds.TargetId, like.TargetId);

        var domainEvent = Assert.IsType<ContentLikedDomainEvent>(
            Assert.Single(like.DomainEvents));
        Assert.Equal(like.UserId, domainEvent.UserId);
        Assert.Equal(like.TargetType, domainEvent.TargetType);
        Assert.Equal(like.TargetId, domainEvent.TargetId);
    }

    [Fact]
    public void MarkForRemoval_ExistingLike_RaisesUnlikedEvent()
    {
        var like = Like.Create(
            SocialTestIds.UserId,
            InteractableType.List,
            SocialTestIds.TargetId);
        like.ClearDomainEvents();

        like.MarkForRemoval();

        var domainEvent = Assert.IsType<ContentUnlikedDomainEvent>(
            Assert.Single(like.DomainEvents));
        Assert.Equal(like.UserId, domainEvent.UserId);
        Assert.Equal(like.TargetType, domainEvent.TargetType);
        Assert.Equal(like.TargetId, domainEvent.TargetId);
    }
}
