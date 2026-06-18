using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Tests.Factories;

public static class BookSnapshotFactory
{
    public static BookSnapshot Create(
        Guid? bookId = null,
        string title = "Clean Code",
        string authorDisplay = "Robert C. Martin",
        string? coverUrl = "https://example.com/clean-code.jpg",
        int? pageCount = 464,
        Guid? workId = null)
    {
        return BookSnapshot.Create(
            bookId ?? LibraryTestIds.BookId,
            title,
            authorDisplay,
            coverUrl,
            pageCount,
            workId);
    }
}
