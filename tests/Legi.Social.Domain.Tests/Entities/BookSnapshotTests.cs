using Legi.Social.Domain.Entities;
using Legi.Social.Domain.Tests.Factories;

namespace Legi.Social.Domain.Tests.Entities;

public class BookSnapshotTests
{
    [Fact]
    public void Create_ValidData_CreatesSnapshot()
    {
        var snapshot = BookSnapshot.Create(
            SocialTestIds.BookId,
            "Dune",
            "Frank Herbert",
            "https://cdn.example.com/dune.png",
            412);

        Assert.Equal(SocialTestIds.BookId, snapshot.BookId);
        Assert.Equal("Dune", snapshot.Title);
        Assert.Equal("Frank Herbert", snapshot.AuthorDisplay);
        Assert.Equal("https://cdn.example.com/dune.png", snapshot.CoverUrl);
        Assert.Equal(412, snapshot.PageCount);
    }

    [Fact]
    public void Update_ValidData_UpdatesSnapshot()
    {
        var snapshot = BookSnapshot.Create(
            SocialTestIds.BookId,
            "Dune",
            "Frank Herbert",
            "https://cdn.example.com/dune.png",
            412);
        var originalUpdatedAt = snapshot.UpdatedAt;

        snapshot.Update(
            "Dune Messiah",
            "Frank Herbert",
            "https://cdn.example.com/dune-messiah.png",
            256);

        Assert.Equal("Dune Messiah", snapshot.Title);
        Assert.Equal("Frank Herbert", snapshot.AuthorDisplay);
        Assert.Equal("https://cdn.example.com/dune-messiah.png", snapshot.CoverUrl);
        Assert.Equal(256, snapshot.PageCount);
        Assert.True(snapshot.UpdatedAt >= originalUpdatedAt);
    }
}
