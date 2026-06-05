using Legi.SharedKernel;
using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class UserProfileTests
{
    [Fact]
    public void Create_ValidData_InitializesProfile()
    {
        var profile = UserProfile.Create(SocialTestIds.UserId, "reader");

        Assert.Equal(SocialTestIds.UserId, profile.UserId);
        Assert.Equal("reader", profile.Username);
        Assert.Null(profile.Bio);
        Assert.Null(profile.AvatarUrl);
        Assert.Null(profile.BannerUrl);
        Assert.Equal(0, profile.FollowersCount);
        Assert.Equal(0, profile.FollowingCount);
    }

    [Fact]
    public void UpdateBio_ValidBio_UpdatesBio()
    {
        var profile = UserProfileFactory.Create();

        profile.UpdateBio("A short public bio");

        Assert.Equal("A short public bio", profile.Bio);
    }

    [Fact]
    public void UpdateBio_BioExceedsMaximumLength_ThrowsDomainException()
    {
        var profile = UserProfileFactory.Create();
        var bio = new string('a', UserProfile.MaxBioLength + 1);

        Assert.Throws<DomainException>(() => profile.UpdateBio(bio));
    }

    [Fact]
    public void UpdateAvatar_ValidUrl_UpdatesAvatar()
    {
        var profile = UserProfileFactory.Create();

        profile.UpdateAvatar("https://cdn.example.com/avatar.png");

        Assert.Equal("https://cdn.example.com/avatar.png", profile.AvatarUrl);
    }

    [Fact]
    public void UpdateBanner_ValidUrl_UpdatesBanner()
    {
        var profile = UserProfileFactory.Create();

        profile.UpdateBanner("https://cdn.example.com/banner.png");

        Assert.Equal("https://cdn.example.com/banner.png", profile.BannerUrl);
    }

    [Fact]
    public void UpdateUsername_ValidUsername_UpdatesUsername()
    {
        var profile = UserProfileFactory.Create();

        profile.UpdateUsername("new-reader");

        Assert.Equal("new-reader", profile.Username);
    }

    [Fact]
    public void IncrementAndDecrementFollowers_ExistingFollowerCount_UpdatesCount()
    {
        var profile = UserProfileFactory.Create();

        profile.IncrementFollowers();
        profile.IncrementFollowers();
        profile.DecrementFollowers();

        Assert.Equal(1, profile.FollowersCount);
    }

    [Fact]
    public void DecrementFollowers_CountIsZero_ThrowsDomainException()
    {
        var profile = UserProfileFactory.Create();

        Assert.Throws<DomainException>(() => profile.DecrementFollowers());
    }

    [Fact]
    public void IncrementAndDecrementFollowing_ExistingFollowingCount_UpdatesCount()
    {
        var profile = UserProfileFactory.Create();

        profile.IncrementFollowing();
        profile.IncrementFollowing();
        profile.DecrementFollowing();

        Assert.Equal(1, profile.FollowingCount);
    }

    [Fact]
    public void DecrementFollowing_CountIsZero_ThrowsDomainException()
    {
        var profile = UserProfileFactory.Create();

        Assert.Throws<DomainException>(() => profile.DecrementFollowing());
    }
}
