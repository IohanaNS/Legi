using Legi.Library.Domain.Tests.Factories;

namespace Legi.Library.Domain.Tests.Entities;

public class BookSnapshotTests
{
    [Fact]
    public void Create_ValidData_CreatesSnapshot()
    {
        var snapshot = BookSnapshotFactory.Create();

        Assert.Equal(LibraryTestIds.BookId, snapshot.BookId);
        Assert.Equal("Clean Code", snapshot.Title);
        Assert.Equal("Robert C. Martin", snapshot.AuthorDisplay);
        Assert.Equal("https://example.com/clean-code.jpg", snapshot.CoverUrl);
        Assert.Equal(464, snapshot.PageCount);
    }

    [Fact]
    public void Update_ValidData_ReplacesSnapshotFields()
    {
        var snapshot = BookSnapshotFactory.Create();
        var previousUpdatedAt = snapshot.UpdatedAt;

        snapshot.Update(
            "The Pragmatic Programmer",
            "Andrew Hunt, David Thomas",
            coverUrl: null,
            pageCount: 352);

        Assert.Equal("The Pragmatic Programmer", snapshot.Title);
        Assert.Equal("Andrew Hunt, David Thomas", snapshot.AuthorDisplay);
        Assert.Null(snapshot.CoverUrl);
        Assert.Equal(352, snapshot.PageCount);
        Assert.True(snapshot.UpdatedAt >= previousUpdatedAt);
    }
}
