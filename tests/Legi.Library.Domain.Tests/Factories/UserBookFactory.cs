using Legi.Library.Domain.Entities;

namespace Legi.Library.Domain.Tests.Factories;

public static class UserBookFactory
{
    public static UserBook Create(
        Guid? userId = null,
        Guid? bookId = null,
        bool wishList = false,
        Guid? workId = null)
    {
        return UserBook.Create(
            userId ?? LibraryTestIds.UserId,
            bookId ?? LibraryTestIds.BookId,
            workId ?? LibraryTestIds.WorkId,
            wishList);
    }
}
