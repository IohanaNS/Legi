using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Enums;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class ContentSnapshotTests
{
    [Fact]
    public void Create_ContentPreviewExceedsMaximumLength_TruncatesPreview()
    {
        var preview = new string('a', ContentSnapshot.MaxContentPreviewLength + 25);

        var snapshot = ContentSnapshotFactory.Create(contentPreview: preview);

        Assert.Equal(ContentSnapshot.MaxContentPreviewLength, snapshot.ContentPreview!.Length);
        Assert.Equal(preview[..ContentSnapshot.MaxContentPreviewLength], snapshot.ContentPreview);
    }

    [Fact]
    public void Create_ValidData_CreatesSnapshot()
    {
        var snapshot = ContentSnapshot.Create(
            InteractableType.List,
            SocialTestIds.TargetId,
            SocialTestIds.UserId,
            "owner",
            "https://cdn.example.com/owner.png",
            "Dune",
            "Frank Herbert",
            "https://cdn.example.com/dune.png",
            "A list preview");

        Assert.Equal(InteractableType.List, snapshot.TargetType);
        Assert.Equal(SocialTestIds.TargetId, snapshot.TargetId);
        Assert.Equal(SocialTestIds.UserId, snapshot.OwnerId);
        Assert.Equal("owner", snapshot.OwnerUsername);
        Assert.Equal("https://cdn.example.com/owner.png", snapshot.OwnerAvatarUrl);
        Assert.Equal("Dune", snapshot.BookTitle);
        Assert.Equal("Frank Herbert", snapshot.BookAuthor);
        Assert.Equal("https://cdn.example.com/dune.png", snapshot.BookCoverUrl);
        Assert.Equal("A list preview", snapshot.ContentPreview);
    }

    [Fact]
    public void UpdateOwner_ValidData_UpdatesOwnerSnapshot()
    {
        var snapshot = ContentSnapshotFactory.Create();

        snapshot.UpdateOwner("renamed-owner", "https://cdn.example.com/new-owner.png");

        Assert.Equal("renamed-owner", snapshot.OwnerUsername);
        Assert.Equal("https://cdn.example.com/new-owner.png", snapshot.OwnerAvatarUrl);
    }
}
