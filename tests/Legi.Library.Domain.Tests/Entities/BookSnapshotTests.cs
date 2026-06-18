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
            pageCount: 352,
            workId: null);

        Assert.Equal("The Pragmatic Programmer", snapshot.Title);
        Assert.Equal("Andrew Hunt, David Thomas", snapshot.AuthorDisplay);
        Assert.Null(snapshot.CoverUrl);
        Assert.Equal(352, snapshot.PageCount);
        Assert.True(snapshot.UpdatedAt >= previousUpdatedAt);
    }

    [Fact]
    public void Update_DoesNotRegressKnownWorkId_WhenWorkIdIsNull()
    {
        var workId = Guid.NewGuid();
        var snapshot = BookSnapshotFactory.Create(workId: workId);

        // An old-style update (no work id) must not wipe a known work id.
        snapshot.Update("New Title", "New Author", coverUrl: null, pageCount: 100, workId: null);

        Assert.Equal(workId, snapshot.WorkId);
    }

    [Fact]
    public void Update_PopulatesWorkId_WhenPreviouslyNull()
    {
        var snapshot = BookSnapshotFactory.Create(workId: null);
        var workId = Guid.NewGuid();

        snapshot.Update("T", "A", coverUrl: null, pageCount: 100, workId: workId);

        Assert.Equal(workId, snapshot.WorkId);
    }
}
