using Legi.SharedKernel;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Events;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class FollowTests
{
    [Fact]
    public void Create_ValidData_CreatesFollowAndRaisesCreatedEvent()
    {
        var follow = Follow.Create(
            SocialTestIds.UserId,
            SocialTestIds.OtherUserId);

        Assert.NotEqual(Guid.Empty, follow.Id);
        Assert.Equal(SocialTestIds.UserId, follow.FollowerId);
        Assert.Equal(SocialTestIds.OtherUserId, follow.FollowingId);

        var domainEvent = Assert.IsType<FollowCreatedDomainEvent>(
            Assert.Single(follow.DomainEvents));
        Assert.Equal(follow.FollowerId, domainEvent.FollowerId);
        Assert.Equal(follow.FollowingId, domainEvent.FollowingId);
    }

    [Fact]
    public void Create_UserFollowsThemselves_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            Follow.Create(SocialTestIds.UserId, SocialTestIds.UserId));
    }

    [Fact]
    public void MarkForRemoval_ExistingFollow_RaisesRemovedEvent()
    {
        var follow = Follow.Create(
            SocialTestIds.UserId,
            SocialTestIds.OtherUserId);
        follow.ClearDomainEvents();

        follow.MarkForRemoval();

        var domainEvent = Assert.IsType<FollowRemovedDomainEvent>(
            Assert.Single(follow.DomainEvents));
        Assert.Equal(follow.FollowerId, domainEvent.FollowerId);
        Assert.Equal(follow.FollowingId, domainEvent.FollowingId);
    }
}
